using MarketBasketAnalysis.Common.Protos;

namespace MarketBasketAnalysis.Server.Application.Services;

public interface IAssociationRuleSetSaver
{
    Task SaveAsync(AssociationRuleSetInfoMessage associationRuleSetInfoMessage,
        IAsyncEnumerable<ItemChunkMessage> itemChunkMessages,
        IAsyncEnumerable<AssociationRuleChunkMessage> associationRuleChunkMessages,
        CancellationToken token);
}
