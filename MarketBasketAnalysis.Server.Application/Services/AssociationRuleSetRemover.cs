using MarketBasketAnalysis.Server.Application.Exceptions;
using MarketBasketAnalysis.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace MarketBasketAnalysis.Server.Application.Services;

public sealed class AssociationRuleSetRemover : IAssociationRuleSetRemover
{
    #region Fields and Properties

    private readonly IDbContextFactory<MarketBasketAnalysisDbContext> _contextFactory;

    #endregion

    #region Constructors

    public AssociationRuleSetRemover(IDbContextFactory<MarketBasketAnalysisDbContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);

        _contextFactory = contextFactory;
    }

    #endregion

    #region Methods

    public async Task RemoveAsync(string associationRuleSetName, CancellationToken token)
    {
        using var context = await _contextFactory.CreateDbContextAsync(token);

        try
        {
            var associationRuleSet = await context.AssociationRuleSets
                .FirstOrDefaultAsync(e => e.IsLoaded && e.Name == associationRuleSetName, token);

            if (associationRuleSet == null)
                throw new AssociationRuleSetNotFoundException(associationRuleSetName);

            context.Remove(associationRuleSet);
            await context.SaveChangesAsync(token);
        }
        catch(Exception e) when (e is DbException or DbUpdateException)
        {
            throw new AssociationRuleSetRemoveException(associationRuleSetName, e);
        }
    }

    #endregion
}
