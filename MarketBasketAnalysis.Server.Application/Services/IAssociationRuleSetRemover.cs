namespace MarketBasketAnalysis.Server.Application.Services;

public interface IAssociationRuleSetRemover
{
    Task RemoveAsync(string associationRuleSetName, CancellationToken token);
}
