namespace MarketBasketAnalysis.Server.Application.Exceptions;

public class AssociationRuleSetSaveException : Exception
{
    public AssociationRuleSetSaveException(string message) : base(message)
    {

    }

    public AssociationRuleSetSaveException(string message, Exception innerException) : base(message, innerException)
    {

    }
}