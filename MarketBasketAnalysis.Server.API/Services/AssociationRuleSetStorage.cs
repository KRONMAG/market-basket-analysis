using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MarketBasketAnalysis.Common.Protos;
using MarketBasketAnalysis.Server.API.Extensions;
using MarketBasketAnalysis.Server.Application.Exceptions;
using MarketBasketAnalysis.Server.Application.Services;
using static MarketBasketAnalysis.Common.Protos.AssociationRuleSetPartMessage;

#pragma warning disable CA1062 // Validate arguments of public methods

namespace MarketBasketAnalysis.Server.API.Services;

public class AssociationRuleSetStorage : Common.Protos.AssociationRuleSetStorage.AssociationRuleSetStorageBase
{
    #region Fields and Properties

    private readonly IAssociationRuleSetInfoLoader _associationRuleSetInfoLoader;
    private readonly IAssociationRuleSetLoader _associationRuleSetLoader;
    private readonly IAssociationRuleSetSaver _associationRuleSetSaver;
    private readonly IAssociationRuleSetRemover _associationRuleSetRemover;
    private readonly ILogger _logger;

    #endregion

    #region Constructors

    public AssociationRuleSetStorage(IAssociationRuleSetInfoLoader associationRuleSetInfoLoader,
        IAssociationRuleSetLoader associationRuleSetLoader, IAssociationRuleSetSaver associationRuleSetSaver,
        IAssociationRuleSetRemover associationRuleSetRemover, ILogger<AssociationRuleSetStorage> logger)
    {
        ArgumentNullException.ThrowIfNull(associationRuleSetInfoLoader);
        ArgumentNullException.ThrowIfNull(associationRuleSetLoader);
        ArgumentNullException.ThrowIfNull(associationRuleSetSaver);
        ArgumentNullException.ThrowIfNull(associationRuleSetRemover);
        ArgumentNullException.ThrowIfNull(logger);

        _associationRuleSetInfoLoader = associationRuleSetInfoLoader;
        _associationRuleSetLoader = associationRuleSetLoader;
        _associationRuleSetSaver = associationRuleSetSaver;
        _associationRuleSetRemover = associationRuleSetRemover;
        _logger = logger;
    }

    #endregion

    #region Methods

    #region Get

    public async override Task<GetResponse> Get(Empty request, ServerCallContext context)
    {
        List<AssociationRuleSetInfoMessage>? associationRuleSetInfos = null;

        try
        {
            associationRuleSetInfos = await _associationRuleSetInfoLoader.LoadAsync(context.CancellationToken);
        }
        catch (AssociationRuleSetLoadException e)
        {
            _logger.LogError(e, "Failed to load association rule set info.");

            RpcThrowHelper.Internal(e.Message);
        }

        var response = new GetResponse();

        response.Values.AddRange(associationRuleSetInfos);

        return response;
    }

    #endregion

    #region Load

    public override async Task Load(LoadRequest request, IServerStreamWriter<LoadResponse> responseStream, ServerCallContext context)
    {
        try
        {
            var associationRuleSetInfo = await _associationRuleSetLoader.LoadAssociationRuleSetInfoAsync(
                request.AssociationRuleSetName, context.CancellationToken);

            await responseStream.WriteAsync(new LoadResponse
            {
                AssociationRuleSetPart = new AssociationRuleSetPartMessage
                {
                    AssociationRuleSetInfo = associationRuleSetInfo
                }
            }, context.CancellationToken);

            var itemChunks = _associationRuleSetLoader.LoadItemChunksAsync(request.AssociationRuleSetName, context.CancellationToken);

            await foreach (var itemChunk in itemChunks)
            {
                await responseStream.WriteAsync(new LoadResponse
                {
                    AssociationRuleSetPart = new AssociationRuleSetPartMessage
                    {
                        ItemChunk = itemChunk
                    }
                }, context.CancellationToken);
            }

            var associationRuleChunks = _associationRuleSetLoader
                .LoadAssociationRuleChunksAsync(request.AssociationRuleSetName, context.CancellationToken);

            await foreach (var associationRuleChunk in associationRuleChunks)
            {
                await responseStream.WriteAsync(new LoadResponse
                {
                    AssociationRuleSetPart = new AssociationRuleSetPartMessage
                    {
                        AssociationRuleChunk = associationRuleChunk
                    }
                }, context.CancellationToken);
            }
        }
        catch (AssociationRuleSetNotFoundException e)
        {
            RpcThrowHelper.NotFound(e.Message);
        }
        catch (AssociationRuleSetLoadException e)
        {
            _logger.LogError(e, "Failed to load association rule set.");

            RpcThrowHelper.Internal(e.Message);
        }
    }

    #endregion

    #region Save

    public override async Task<Empty> Save(IAsyncStreamReader<SaveRequest> requestStream, ServerCallContext context)
    {
        try
        {
            await _associationRuleSetSaver.SaveAsync(
                await ReceiveAssociationRuleSet(requestStream, context),
                ReceiveItemChunks(requestStream, context),
                ReceiveAssociationRuleChunks(requestStream, context),
                context.CancellationToken);
        }
        catch (AssociationRuleSetValidationException e)
        {
            _logger.LogInformation(e, "Validation error occured while loading association rule set.");

            RpcThrowHelper.InvalidArgument(e.Message);
        }
        catch (AssociationRuleSetSaveException e)
        {
            _logger.LogError(e, "Failed to save association rule set.");

            RpcThrowHelper.Internal(e.Message);
        }

        return new();
    }

    private static async Task<AssociationRuleSetInfoMessage> ReceiveAssociationRuleSet(IAsyncStreamReader<SaveRequest> requestStream, ServerCallContext context)
    {
        if (!await requestStream.MoveNext(context.CancellationToken))
            RpcThrowHelper.InvalidArgument("Expected association rule set, but request stream is empty.");

        var part = requestStream.Current.AssociationRuleSetPart;

        CheckPartyType(part.PartTypeCase, PartTypeOneofCase.AssociationRuleSetInfo);

        return part.AssociationRuleSetInfo;
    }

    private static async IAsyncEnumerable<ItemChunkMessage> ReceiveItemChunks(IAsyncStreamReader<SaveRequest> requestStream, ServerCallContext context)
    {
        var atLeastOneItemChunkReceived = false;
        PartTypeOneofCase? partType = null;

        while (await requestStream.MoveNext(context.CancellationToken) &&
               (partType = requestStream.Current.AssociationRuleSetPart.PartTypeCase) == PartTypeOneofCase.ItemChunk)
        {
            yield return requestStream.Current.AssociationRuleSetPart.ItemChunk;

            atLeastOneItemChunkReceived = true;
        }

        if (partType == null)
            RpcThrowHelper.InvalidArgument("Request stream is empty, association rule set received and item chunks were expected.");

        if (!atLeastOneItemChunkReceived)
            CheckPartyType(partType.Value, PartTypeOneofCase.ItemChunk);
    }

    private static async IAsyncEnumerable<AssociationRuleChunkMessage> ReceiveAssociationRuleChunks(
        IAsyncStreamReader<SaveRequest> requestStream, ServerCallContext context)
    {
        AssociationRuleSetPartMessage? part = null;

        try
        {
            part = requestStream.Current.AssociationRuleSetPart;
        }
        catch (InvalidOperationException)
        {
            RpcThrowHelper.InvalidArgument("Expected association rule chunk, but request stream is empty.");
        }

        CheckPartyType(part.PartTypeCase, PartTypeOneofCase.AssociationRuleChunk);

        yield return part.AssociationRuleChunk;

        while (await requestStream.MoveNext(context.CancellationToken))
        {
            part = requestStream.Current.AssociationRuleSetPart;

            CheckPartyType(part.PartTypeCase, PartTypeOneofCase.AssociationRuleChunk);

            yield return part.AssociationRuleChunk;
        }
    }

    private static void CheckPartyType(PartTypeOneofCase actualPartType, PartTypeOneofCase expectedPartType)
    {
        if (actualPartType == expectedPartType)
            return;

        var actualPartTypeName = GetPartTypeName(actualPartType);
        var expectedPartTypeName = GetPartTypeName(expectedPartType);

        RpcThrowHelper.InvalidArgument($"Expected {expectedPartTypeName}, but received {actualPartTypeName}.");
    }

    private static string GetPartTypeName(PartTypeOneofCase partType) =>
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
        partType switch
#pragma warning restore CS8524
        {
            PartTypeOneofCase.None => "none",
            PartTypeOneofCase.AssociationRuleSetInfo => "association rule set info",
            PartTypeOneofCase.ItemChunk => "item chunk",
            PartTypeOneofCase.AssociationRuleChunk => "association rule chunk"
        };

    #endregion

    #region Remove

    public override async Task<Empty> Remove(RemoveRequest request, ServerCallContext context)
    {
        try
        {
            await _associationRuleSetRemover.RemoveAsync(request.AssociationRuleSetName, context.CancellationToken);
        }
        catch (AssociationRuleSetNotFoundException e)
        {
            RpcThrowHelper.NotFound(e.Message);
        }
        catch (AssociationRuleSetRemoveException e)
        {
            _logger.LogError(e, "Failed to remove association rule set.");

            RpcThrowHelper.Internal(e.Message);
        }

        return new();
    }

    #endregion

    #endregion
}