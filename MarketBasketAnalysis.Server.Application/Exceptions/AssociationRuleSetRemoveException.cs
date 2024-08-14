namespace MarketBasketAnalysis.Server.Application.Exceptions;

public class AssociationRuleSetRemoveException(string associationRuleSetName, Exception innerException)
    : Exception(
        $"Unexpected error occurred while removing association set with name ${associationRuleSetName}.",
        innerException);