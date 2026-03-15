namespace MesTech.Domain.Services;

/// <summary>
/// Buybox izleme servisi arayuzu.
/// Kendi fiyatini rakip fiyatlarla karsilastirarak rekabetcilik analizi yapar.
/// Implementasyon Infrastructure katmaninda olacak (dis veri kaynaklari gerektirir).
/// </summary>
public interface IBuyboxMonitorService
{
    /// <summary>
    /// Belirtilen urun icin buybox analizi yapar.
    /// Kendi fiyat ile rakip fiyatlari karsilastirir ve oneri uretir.
    /// </summary>
    BuyboxAnalysis Analyze(BuyboxInput input);

    /// <summary>
    /// Birden fazla urun icin toplu buybox analizi.
    /// </summary>
    IReadOnlyList<BuyboxAnalysis> AnalyzeBatch(IEnumerable<BuyboxInput> inputs);
}

/// <summary>
/// Buybox analiz girdi modeli.
/// </summary>
public record BuyboxInput(
    string SKU,
    decimal OwnPrice,
    IReadOnlyList<CompetitorPrice> CompetitorPrices);

/// <summary>
/// Rakip fiyat bilgisi.
/// </summary>
public record CompetitorPrice(
    string CompetitorName,
    decimal Price,
    bool IsInStock = true);

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
