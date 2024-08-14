namespace MarketBasketAnalysis.Server.Data;

public class ItemChunk
{
    public int Id { get; set; }

#pragma warning disable CA1819 // Properties should not return arrays
    public byte[] Data { get; set; } = null!;
#pragma warning restore CA1819

    public int PayloadSize { get; set; }

    public int AssociationRuleSetId { get; set; }

    public AssociationRuleSet? AssociationRuleSet { get; set; }
}
