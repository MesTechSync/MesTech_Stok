namespace MesTech.Application.Interfaces;

public interface IBuyboxService
{
    Task<BuyboxAnalysis> AnalyzeCompetitorsAsync(
        string sku, decimal currentPrice, string platformCode,
        CancellationToken ct = default);

    Task<IReadOnlyList<BuyboxPosition>> CheckBuyboxPositionsAsync(
        Guid tenantId, string platformCode,
        CancellationToken ct = default);

    Task<IReadOnlyList<BuyboxLostItem>> GetLostBuyboxesAsync(
        Guid tenantId, CancellationToken ct = default);
}

public record BuyboxAnalysis(
    bool HasBuybox,
    decimal CurrentPrice,
    decimal LowestCompetitorPrice,
    string LowestCompetitorName,
    IReadOnlyList<CompetitorPriceInfo> Competitors,
    decimal SuggestedPrice,
    string Reasoning);

public record CompetitorPriceInfo(
    string SellerName,
    decimal Price,
    decimal ShippingCost,
    int SellerRating,
    bool HasBuybox,
    DateTime LastChecked);

public record BuyboxPosition(
    Guid ProductId,
    string SKU,
    bool HasBuybox,
    decimal CurrentPrice,
    decimal BuyboxPrice,
    decimal PriceDiff);

public record BuyboxLostItem(
    Guid ProductId,
    string SKU,
    string ProductName,
    decimal CurrentPrice,
    decimal CompetitorPrice,
    string CompetitorName,
    DateTime LostAt);
