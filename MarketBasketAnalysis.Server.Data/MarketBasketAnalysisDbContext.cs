using Microsoft.EntityFrameworkCore;

namespace MarketBasketAnalysis.Server.Data;

public class MarketBasketAnalysisDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<AssociationRuleSet> AssociationRuleSets { get; set; }

    public DbSet<AssociationRuleChunk> AssociationRuleChunks { get; set; }

    public DbSet<ItemChunk> ItemChunks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AssociationRuleSet>().HasIndex(e => e.Name).IsUnique();
    }
}