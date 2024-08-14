namespace MarketBasketAnalysis.Server.Application.Exceptions;

public class AssociationRuleSetNotFoundException(string associationRuleSetName)
    : Exception($"Association rule set with name \"{associationRuleSetName}\" not found.");