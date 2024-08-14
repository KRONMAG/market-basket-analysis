using MarketBasketAnalysis.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketBasketAnalysis.Server.API.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ConfigureDb(this WebApplication webApplication)
    {
        ArgumentNullException.ThrowIfNull(webApplication);

        using var scope = webApplication.Services.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MarketBasketAnalysisDbContext>>();
        await using var context = await contextFactory.CreateDbContextAsync();

        await context.Database.MigrateAsync();

        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=True;");
    }
}