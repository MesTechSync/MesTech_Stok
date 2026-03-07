namespace MesTech.Application.Interfaces;

/// <summary>
/// MESA OS AI servis kontrati.
/// Dalga 1: MockMesaAIService (sahte veri).
/// Dalga 2+: RealMesaAIClient (HTTP → MESA OS Fastify API).
/// </summary>
public interface IMesaAIService
{
    /// <summary>Urun aciklamasi uretir (Trendyol/HB formatinda).</summary>
    Task<AiContentResult> GenerateProductDescriptionAsync(
        string sku,
        string productName,
        string? category,
        List<string>? imageUrls,
        CancellationToken ct = default);

    /// <summary>Urun gorselleri uretir (kaynak fotograf → profesyonel gorseller).</summary>
    Task<AiImageResult> GenerateProductImagesAsync(
        string sku,
        List<string> sourceImageUrls,
        CancellationToken ct = default);

    /// <summary>Rakip analizine gore fiyat onerisi.</summary>
    Task<AiPriceRecommendation> RecommendPriceAsync(
        string sku,
        decimal currentPrice,
        List<CompetitorPrice>? competitors,
        CancellationToken ct = default);

    /// <summary>Zaman serisi + gecmis veriye gore stok tahmini.</summary>
    Task<AiStockPrediction> PredictStockAsync(
        string sku,
        int currentStock,
        List<StockHistoryPoint>? history,
        CancellationToken ct = default);

    /// <summary>Kategori esleme onerisi (platformlar arasi).</summary>
    Task<AiCategoryMapping> SuggestCategoryAsync(
        string productName,
        string? description,
        string targetPlatform,
        CancellationToken ct = default);
}

// ── Result Types ──

/// <summary>AI icerik uretim sonucu.</summary>
public record AiContentResult(
    bool Success,
    string? Content,
    string? ErrorMessage,
    Dictionary<string, string>? Metadata);

/// <summary>AI gorsel uretim sonucu.</summary>
public record AiImageResult(
    bool Success,
    List<string> ImageUrls,
    string? ErrorMessage);

/// <summary>AI fiyat onerisi sonucu.</summary>
public record AiPriceRecommendation(
    bool Success,
    decimal RecommendedPrice,
    decimal MinPrice,
    decimal MaxPrice,
    string? Reasoning);

/// <summary>AI stok tahmin sonucu.</summary>
public record AiStockPrediction(
    bool Success,
    int PredictedDemand,
    int ReorderSuggestion,
    int DaysUntilStockout,
    string? Reasoning);

/// <summary>AI kategori esleme sonucu.</summary>
public record AiCategoryMapping(
    bool Success,
    string SuggestedCategoryId,
    string SuggestedCategoryName,
    double Confidence);

// ── Input Types ──

/// <summary>Rakip fiyat bilgisi.</summary>
public record CompetitorPrice(
    string CompetitorName,
    decimal Price,
    string? Url);

/// <summary>Stok gecmis verisi (zaman serisi noktasi).</summary>
public record StockHistoryPoint(
    DateTime Date,
    int Quantity,
    int SoldCount);
