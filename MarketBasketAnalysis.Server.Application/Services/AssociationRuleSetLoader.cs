using MarketBasketAnalysis.Common.Protos;
using MarketBasketAnalysis.Server.Application.Exceptions;
using MarketBasketAnalysis.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace MarketBasketAnalysis.Server.Application.Services;

public sealed class AssociationRuleSetLoader : IAssociationRuleSetLoader
{
    #region Fields and properties

    private readonly IDbContextFactory<MarketBasketAnalysisDbContext> _contextFactory;

    private MarketBasketAnalysisDbContext? _context;

    private bool _disposed;

    #endregion

    #region Constructors

    public AssociationRuleSetLoader(IDbContextFactory<MarketBasketAnalysisDbContext> contexFactory)
    {
        ArgumentNullException.ThrowIfNull(contexFactory);

        _contextFactory = contexFactory;
    }

    #endregion

    #region Methods

    public async Task<AssociationRuleSetInfoMessage> LoadAssociationRuleSetInfoAsync(
        string associationRuleSetName, CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        CheckAssociationRuleSetName(associationRuleSetName, nameof(associationRuleSetName));

        await CreateContextIfNeedAsync();

        var associationRuleSet = await _context!.AssociationRuleSets
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Name == associationRuleSetName, token);

        if (associationRuleSet == null)
            throw new AssociationRuleSetNotFoundException(associationRuleSetName);

        return new AssociationRuleSetInfoMessage
        {
            Name = associationRuleSet.Name,
            Description = associationRuleSet.Description,
            TransactionCount = associationRuleSet.TransactionCount
        };
    }

    public IAsyncEnumerable<ItemChunkMessage> LoadItemChunksAsync(string associationRuleSetName,
        CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        CheckAssociationRuleSetName(associationRuleSetName, nameof(associationRuleSetName));

        return AsyncEnumerableEx
            .Defer(() => LoadItemsChunkInernalAsync(associationRuleSetName, token))
            .Catch<ItemChunkMessage, DbException>((e, _) =>
                throw new AssociationRuleSetLoadException(
                    "Unexpected error occurred while loading item chunks.", e));
    }

    private async IAsyncEnumerable<ItemChunkMessage> LoadItemsChunkInernalAsync(
        string associationRuleSetName,
        [EnumeratorCancellation] CancellationToken token)
    {
        await CreateContextIfNeedAsync();

        var itemChunks = _context!.ItemChunks
            .AsNoTracking()
            .Where(e => e.AssociationRuleSet!.Name == associationRuleSetName)
            .AsAsyncEnumerable()
            .WithCancellation(token);

        await foreach (var itemChunk in itemChunks)
        {
            var itemChunkMessage = ItemChunkMessage.Parser.ParseFrom(
                itemChunk.Data, 0, itemChunk.PayloadSize);

            yield return itemChunkMessage;
        }
    }

    public IAsyncEnumerable<AssociationRuleChunkMessage> LoadAssociationRuleChunksAsync(
        string associationRuleSetName, CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        CheckAssociationRuleSetName(associationRuleSetName, nameof(associationRuleSetName));

        return AsyncEnumerableEx
            .Defer(() => LoadAssociationRuleChunksInternalAsync(associationRuleSetName, token))
            .Catch<AssociationRuleChunkMessage, DbException>((e, _) =>
                throw new AssociationRuleSetLoadException(
                    "Unexpected error occurred while loading association rule chunks.", e));
    }

    private async IAsyncEnumerable<AssociationRuleChunkMessage> LoadAssociationRuleChunksInternalAsync(
        string associationRuleSetName, [EnumeratorCancellation] CancellationToken token)
    {
        await CreateContextIfNeedAsync();

        var associationRuleChunks = _context!.AssociationRuleChunks
            .AsNoTracking()
            .Where(e => e.AssociationRuleSet!.Name == associationRuleSetName)
            .AsAsyncEnumerable()
            .WithCancellation(token);

        await foreach (var associationRuleChunk in associationRuleChunks)
        {
            var itemChunkMessage = AssociationRuleChunkMessage.Parser.ParseFrom(
                associationRuleChunk.Data, 0, associationRuleChunk.PayloadSize);

            yield return itemChunkMessage;
        }
    }

    private static void CheckAssociationRuleSetName(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Association rule set name cannot be null, empty or composed entirely of whitespace.",
                paramName);
        }
    }

    private async Task CreateContextIfNeedAsync() =>
        _context ??= await _contextFactory.CreateDbContextAsync();

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        if (_context != null)
            await _context.DisposeAsync();

        _disposed = true;
    }

    #endregion
}
