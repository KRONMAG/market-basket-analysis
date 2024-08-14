using MarketBasketAnalysis.Common.Protos;

namespace MarketBasketAnalysis.Server.Application.Services;

public interface IAssociationRuleSetLoader : IAsyncDisposable
{
    Task<AssociationRuleSetInfoMessage> LoadAssociationRuleSetInfoAsync(string associationRuleSetName, CancellationToken token);

    IAsyncEnumerable<ItemChunkMessage> LoadItemChunksAsync(string associationRuleSetName, CancellationToken token);

    IAsyncEnumerable<AssociationRuleChunkMessage> LoadAssociationRuleChunksAsync(string associationRuleSetName, CancellationToken token);
}
