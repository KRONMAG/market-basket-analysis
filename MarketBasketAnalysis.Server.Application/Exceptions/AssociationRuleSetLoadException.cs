namespace MarketBasketAnalysis.Server.Application.Exceptions;

public class AssociationRuleSetLoadException(string message, Exception innerException) : Exception(message, innerException);