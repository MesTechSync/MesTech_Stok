namespace MesTech.Domain.Services;

/// <summary>
/// Buybox analiz sonucu.
/// </summary>
public record BuyboxAnalysis
{
    public string SKU { get; init; } = string.Empty;
    public decimal OwnPrice { get; init; }
    public bool IsCompetitive { get; init; }
    public decimal? SuggestedPrice { get; init; }
    public decimal? PriceGap { get; init; }
    public decimal? PriceGapPercent { get; init; }
    public string? CheapestCompetitor { get; init; }
    public decimal? CheapestCompetitorPrice { get; init; }
    public int CompetitorCount { get; init; }
    public IReadOnlyList<CompetitorPrice> Competitors { get; init; } = Array.Empty<CompetitorPrice>();
}
