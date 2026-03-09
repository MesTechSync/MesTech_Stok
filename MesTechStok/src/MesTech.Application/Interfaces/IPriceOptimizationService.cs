namespace MesTech.Application.Interfaces;

public interface IPriceOptimizationService
{
    Task<PriceOptimization> OptimizePriceAsync(
        Guid productId, decimal currentPrice, decimal costPrice,
        string platformCode, CancellationToken ct = default);

    Task<IReadOnlyList<PriceOptimization>> OptimizeBulkAsync(
        Guid tenantId, string? platformCode = null,
        string? categoryId = null,
        CancellationToken ct = default);

    Task<PriceHistory> GetPriceHistoryAsync(
        Guid productId, int days = 30,
        CancellationToken ct = default);
}

public record PriceOptimization(
    Guid ProductId,
    string SKU,
    decimal CurrentPrice,
    decimal CostPrice,
    decimal RecommendedPrice,
    decimal MinPrice,
    decimal MaxPrice,
    decimal ExpectedMargin,
    double Confidence,
    PriceStrategy Strategy,
    string Reasoning);

public enum PriceStrategy { Competitive, MarginMaximize, BuyboxWin, Clearance }

public record PriceHistory(
    Guid ProductId,
    string SKU,
    IReadOnlyList<PricePoint> PricePoints,
    IReadOnlyList<PricePoint> AiRecommendations,
    decimal AveragePrice,
    decimal AverageAiPrice);

public record PricePoint(DateTime Date, decimal Price, string? Source);
