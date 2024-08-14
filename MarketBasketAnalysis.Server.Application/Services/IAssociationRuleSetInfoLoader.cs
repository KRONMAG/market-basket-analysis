using MarketBasketAnalysis.Common.Protos;

namespace MarketBasketAnalysis.Server.Application.Services;

public interface IAssociationRuleSetInfoLoader
{
    Task<List<AssociationRuleSetInfoMessage>> LoadAsync(CancellationToken token);
}
