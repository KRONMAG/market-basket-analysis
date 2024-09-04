using Google.Protobuf;
using MarketBasketAnalysis.Common.Protos;
using MarketBasketAnalysis.Server.Application.Exceptions;
using MarketBasketAnalysis.Server.Application.Extensions;
using MarketBasketAnalysis.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using System.Buffers;
using System.Data.Common;

namespace MarketBasketAnalysis.Server.Application.Services;

public sealed class AssociationRuleSetSaver : IAssociationRuleSetSaver
{
    #region Fields and Properties

    private const int RollbackChangesRetryCount = 5;
    private const int RollbackChangesInitialBackoff = 2;
    private const double RollbackChangesBackoffMultiplier = 1.5;

    private readonly IDbContextFactory<MarketBasketAnalysisDbContext> _contextFactory;
    private readonly ILogger<AssociationRuleSetSaver> _logger;

    private MarketBasketAnalysisDbContext? _context;
    private Dictionary<int, int>? _itemCounts;

    #endregion

    #region Constructors

    public AssociationRuleSetSaver(IDbContextFactory<MarketBasketAnalysisDbContext> contextFactory,
        ILogger<AssociationRuleSetSaver> logger)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _contextFactory = contextFactory;
        _logger = logger;
    }

    #endregion

    #region Methods

    public async Task SaveAsync(AssociationRuleSetInfoMessage associationRuleSetInfoMessage,
        IAsyncEnumerable<ItemChunkMessage> itemChunkMessages,
        IAsyncEnumerable<AssociationRuleChunkMessage> associationRuleChunkMessages,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(associationRuleSetInfoMessage);
        ArgumentNullException.ThrowIfNull(itemChunkMessages);
        ArgumentNullException.ThrowIfNull(associationRuleChunkMessages);

        _context = await _contextFactory.CreateDbContextAsync(token);
        _itemCounts = new Dictionary<int, int>();

        AssociationRuleSet? associationRuleSet = null;

        try
        {
            associationRuleSet = await SaveAssociationRuleSetInfo(associationRuleSetInfoMessage, token);

            await SaveItemChunks(itemChunkMessages, associationRuleSet, token);
            await SaveAssociationRuleChunks(associationRuleChunkMessages, associationRuleSet, token);
            await MarkAssociationRuleSetAsLoaded(associationRuleSet, token);
        }
        catch (Exception e) when (ShouldHandleException(e))
        {
            if (associationRuleSet != null)
            {
                try
                {
                    await RollbackChanges(associationRuleSet);
                }
                catch (DbException de)
                {
                    _logger.LogError(de, "Failed to rollback changes made while saving association rule set.");
                }
            }

            throw;
        }
        finally
        {
            await _context.DisposeAsync();

            _context = null;
            _itemCounts = null;
        }
    }

    private static bool ShouldHandleException(Exception e) =>
        e is AssociationRuleSetValidationException or AssociationRuleSetSaveException or OperationCanceledException;

    private async Task<AssociationRuleSet> SaveAssociationRuleSetInfo(AssociationRuleSetInfoMessage associationRuleSetMessage, CancellationToken token)
    {
        associationRuleSetMessage.Name.CheckAssociationRuleSetName();

        if (associationRuleSetMessage.TransactionCount <= 0)
        {
            throw new AssociationRuleSetValidationException(
                "Association rule set should have positive transaction count.");
        }

        var isAlreadyExists = await _context!.AssociationRuleSets
            .AnyAsync(e => e.Name == associationRuleSetMessage.Name, token);

        if (isAlreadyExists)
        {
            throw new AssociationRuleSetValidationException(
                $"Association rule set with name \"{associationRuleSetMessage.Name}\" already exists.");
        }

        var associationRuleSet = new AssociationRuleSet
        {
            Name = associationRuleSetMessage.Name,
            Description = associationRuleSetMessage.Description,
            TransactionCount = associationRuleSetMessage.TransactionCount
        };

        await _context.AddAsync(associationRuleSet, token);

        try
        {
            await _context.SaveChangesAsync(token);
        }
        catch (DbUpdateException e)
        {
            throw new AssociationRuleSetSaveException(
                "Unexpected error occured while saving association rule set info.", e);
        }
        finally
        {
            _context.ChangeTracker.Clear();
        }

        return associationRuleSet;
    }

    private async Task SaveItemChunks(IAsyncEnumerable<ItemChunkMessage> itemChunkMessages,
        AssociationRuleSet associationRuleSet, CancellationToken token)
    {
        await foreach (var itemChunkMessage in itemChunkMessages.WithCancellation(token))
        {
            foreach (var itemMessage in itemChunkMessage.Values)
            {
                if (itemMessage.Name == null)
                    throw new AssociationRuleSetValidationException($"Item with ID {itemMessage.Id} should have non-null name.");

                if (itemMessage.Count < 0)
                    throw new AssociationRuleSetValidationException($"Count of item with ID {itemMessage.Id} should be positive.");

                if (_itemCounts!.ContainsKey(itemMessage.Id))
                    throw new AssociationRuleSetValidationException($"Item with ID {itemMessage.Id} is duplicated.");

                if (itemMessage.Count > associationRuleSet.TransactionCount)
                {
                    throw new AssociationRuleSetValidationException(
                        $"Count of item  with ID {itemMessage.Id} should be less than or equal to transaction count.");
                }

                _itemCounts!.Add(itemMessage.Id, itemMessage.Count);
            }

            await SaveChunkMessageAsync(itemChunkMessage, (data, payloadSize) => new ItemChunk
            {
                Data = data,
                PayloadSize = payloadSize,
                AssociationRuleSetId = associationRuleSet.Id
            }, token);
        }
    }

    private async Task SaveAssociationRuleChunks(IAsyncEnumerable<AssociationRuleChunkMessage> associationRuleChunkMessages,
        AssociationRuleSet associationRuleSet, CancellationToken token)
    {
        await foreach (var associationRuleChunkMessage in associationRuleChunkMessages.WithCancellation(token))
        {
            foreach (var associationRuleMessage in associationRuleChunkMessage.Values)
            {
                var leftHandSideId = associationRuleMessage.LeftHandSideId;
                var rightHandSideId = associationRuleMessage.RightHandSideId;
                var handSidesCount = associationRuleMessage.Count;

                if (leftHandSideId == rightHandSideId)
                {
                    throw new AssociationRuleSetValidationException(
                        $"Left and right hand side IDs should be different, but they are equal to {leftHandSideId}.");
                }

                if (associationRuleMessage.Count <= 0)
                {
                    throw new AssociationRuleSetValidationException(
                        $"Count of association rule with hand side IDs {leftHandSideId} and {rightHandSideId} should be positive.");
                }

                if (!_itemCounts!.TryGetValue(leftHandSideId, out var leftHandSideCount))
                {
                    throw new AssociationRuleSetValidationException(
                        $"Left hand side with ID {leftHandSideId} not found in items list.");
                }

                if (!_itemCounts!.TryGetValue(rightHandSideId, out var rightHandSideCount))
                {
                    throw new AssociationRuleSetValidationException(
                        $"Right hand side with ID {rightHandSideId} not found in items list.");
                }

                if (handSidesCount > leftHandSideCount)
                {
                    throw new AssociationRuleSetValidationException(
                        $"Count of association rule with hand side IDs {leftHandSideId} " +
                        $"and {rightHandSideId} should be less than or equal to left hand side count.");
                }

                if (handSidesCount > rightHandSideCount)
                {
                    throw new AssociationRuleSetValidationException(
                        $"Count of association rule with hand side IDs {leftHandSideId} " +
                        $"and {rightHandSideId} should be less than or equal to right hand side count.");
                }
            }

            await SaveChunkMessageAsync(associationRuleChunkMessage,
                (data, payloadSize) => new AssociationRuleChunk
                {
                    Data = data,
                    PayloadSize = payloadSize,
                    AssociationRuleSetId = associationRuleSet.Id
                }, token);
        }
    }

    private async Task SaveChunkMessageAsync<TEntity, TMessage>(TMessage message, Func<byte[], int, TEntity> entityFactory,
        CancellationToken token)
        where TEntity : notnull
        where TMessage : IMessage
    {
        var messageSize = message.CalculateSize();
        var buffer = ArrayPool<byte>.Shared.Rent(messageSize);

        try
        {
            var bufferSegment = new ArraySegment<byte>(buffer, 0, messageSize);

            message.WriteTo(bufferSegment);

            var entity = entityFactory(buffer, messageSize);

            await _context!.AddAsync(entity, token);
            await _context.SaveChangesAsync(token);
        }
        catch (Exception e) when (e is DbException or DbUpdateException)
        {
            var chunkMessageName = message is ItemChunkMessage ? "item" : "association rule";
            var errorMessage = $"Unexpected error occured while saving {chunkMessageName} chunk.";

            throw new AssociationRuleSetSaveException(errorMessage, e);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);

            _context!.ChangeTracker.Clear();
        }
    }

    private async Task MarkAssociationRuleSetAsLoaded(AssociationRuleSet associationRuleSet, CancellationToken token)
    {
        associationRuleSet.IsLoaded = true;

        _context!.Entry(associationRuleSet).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync(token);
        }
        finally
        {
            _context.ChangeTracker.Clear();
        }
    }

    private async Task RollbackChanges(AssociationRuleSet associationRuleSet) =>
        await Policy.Handle<DbException>()
            .WaitAndRetryAsync(RollbackChangesRetryCount, n =>
            {
                var durationInSeconds = RollbackChangesInitialBackoff * Math.Pow(RollbackChangesBackoffMultiplier, n - 1);

                return TimeSpan.FromSeconds(durationInSeconds);
            })
            .ExecuteAsync(async () =>
            {
                _context!.AssociationRuleSets.Entry(associationRuleSet).State = EntityState.Deleted;

                await _context.SaveChangesAsync();
            });

    #endregion
}