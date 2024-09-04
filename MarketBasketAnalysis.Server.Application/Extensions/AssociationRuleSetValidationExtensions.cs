using MarketBasketAnalysis.Server.Application.Exceptions;

namespace MarketBasketAnalysis.Server.Application.Extensions;

internal static class AssociationRuleSetValidationExtensions
{
    internal static void CheckAssociationRuleSetName(this string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AssociationRuleSetValidationException(
                "Association rule set name cannot be null, empty or composed entirely of whitespace.");
        }
    }
}
