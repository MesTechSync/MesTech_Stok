namespace MesTech.Application.Interfaces;

public interface IStockPredictionService
{
    Task<StockForecast> PredictDemandAsync(
        Guid productId, int forecastDays = 30,
        CancellationToken ct = default);

    Task<IReadOnlyList<StockAlert>> GetStockAlertsAsync(
        Guid tenantId, int daysThreshold = 14,
        CancellationToken ct = default);

    Task<ReorderSuggestion> SuggestReorderAsync(
        Guid productId, CancellationToken ct = default);
}

public record StockForecast(
    Guid ProductId,
    string SKU,
    int CurrentStock,
    int PredictedDemand7d,
    int PredictedDemand14d,
    int PredictedDemand30d,
    int DaysUntilStockout,
    double Confidence,
    string Reasoning);

public record StockAlert(
    Guid ProductId,
    string SKU,
    string ProductName,
    int CurrentStock,
    int DaysUntilStockout,
    int ReorderSuggestion,
    AlertSeverity Severity);

public enum AlertSeverity { Low, Medium, High, Critical }

public record ReorderSuggestion(
    Guid ProductId,
    string SKU,
    int SuggestedQuantity,
    DateTime SuggestedOrderDate,
    decimal EstimatedCost,
    string Reasoning);
