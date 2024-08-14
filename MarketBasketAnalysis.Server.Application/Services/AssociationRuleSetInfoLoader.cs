using MarketBasketAnalysis.Common.Protos;
using MarketBasketAnalysis.Server.Application.Exceptions;
using MarketBasketAnalysis.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace MarketBasketAnalysis.Server.Application.Services;

public class AssociationRuleSetInfoLoader : IAssociationRuleSetInfoLoader
{
    #region Fields and Properties

    private readonly IDbContextFactory<MarketBasketAnalysisDbContext> _contextFactory;

    #endregion

    #region Constructors

    public AssociationRuleSetInfoLoader(IDbContextFactory<MarketBasketAnalysisDbContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);

        _contextFactory = contextFactory;
    }

    #endregion

    #region Methods

    public async Task<List<AssociationRuleSetInfoMessage>> LoadAsync(CancellationToken token)
    {
        using var context = await _contextFactory.CreateDbContextAsync(token);

        try
        {
            return await context.AssociationRuleSets
                .AsNoTracking()
                .Where(e => e.IsLoaded)
                .Select(e => new AssociationRuleSetInfoMessage
                {
                    Name = e.Name,
                    Description = e.Description,
                    TransactionCount = e.TransactionCount
                })
                .ToListAsync(token);
        }
        catch (DbException e)
        {
            throw new AssociationRuleSetLoadException(
                "Unexpected error occurred while loading association rule set info.", e);
        }
    }

    #endregion
}
