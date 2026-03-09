using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI;

public class MockStockPredictionService : IStockPredictionService
{
    private readonly ILogger<MockStockPredictionService> _logger;

    public MockStockPredictionService(ILogger<MockStockPredictionService> logger)
    {
        _logger = logger;
    }

    public Task<StockForecast> PredictDemandAsync(
        Guid productId, int forecastDays = 30, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] StockPrediction.PredictDemand: productId={ProductId}, gun={Days}",
            productId, forecastDays);

        var hash = Math.Abs(productId.GetHashCode());
        var dailyDemand = Math.Max(1, (hash % 20) + 1);
        var currentStock = 50 + (hash % 200);

        var forecast = new StockForecast(
            productId,
            $"SKU-{hash % 10000:D4}",
            currentStock,
            PredictedDemand7d: dailyDemand * 7,
            PredictedDemand14d: dailyDemand * 14,
            PredictedDemand30d: dailyDemand * 30,
            DaysUntilStockout: dailyDemand > 0 ? currentStock / dailyDemand : 999,
            Confidence: currentStock > 100 ? 0.85 : currentStock > 20 ? 0.70 : 0.55,
            Reasoning: currentStock > 100
                ? "Yeterli stok mevcut. Mevcut satis hizinda rahat tedarik suresi var."
                : currentStock > 20
                    ? "Stok azaliyor. 2 hafta icinde yeniden siparis onerilir."
                    : "Kritik stok seviyesi! Acil tedarik gerekli.");

        return Task.FromResult(forecast);
    }

    public Task<IReadOnlyList<StockAlert>> GetStockAlertsAsync(
        Guid tenantId, int daysThreshold = 14, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] StockPrediction.GetAlerts: tenant={TenantId}, esik={Days} gun",
            tenantId, daysThreshold);

        var alerts = new List<StockAlert>
        {
            new(Guid.NewGuid(), "SKU-ALERT-001", "Bluetooth Kulaklik TWS",
                8, DaysUntilStockout: 3, ReorderSuggestion: 100, AlertSeverity.Critical),
            new(Guid.NewGuid(), "SKU-ALERT-002", "USB-C Sarj Kablosu 2m",
                25, DaysUntilStockout: 10, ReorderSuggestion: 200, AlertSeverity.High),
            new(Guid.NewGuid(), "SKU-ALERT-003", "Laptop Stand Aluminyum",
                45, DaysUntilStockout: 18, ReorderSuggestion: 50, AlertSeverity.Medium)
        };

        IReadOnlyList<StockAlert> filtered = alerts
            .Where(a => a.DaysUntilStockout <= daysThreshold)
            .ToList();

        return Task.FromResult(filtered);
    }

    public Task<ReorderSuggestion> SuggestReorderAsync(
        Guid productId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] StockPrediction.SuggestReorder: productId={ProductId}", productId);

        var hash = Math.Abs(productId.GetHashCode());
        var quantity = ((hash % 10) + 1) * 25;
        var unitCost = 20m + (hash % 50);

        var suggestion = new ReorderSuggestion(
            productId,
            $"SKU-{hash % 10000:D4}",
            SuggestedQuantity: quantity,
            SuggestedOrderDate: DateTime.UtcNow.AddDays(3),
            EstimatedCost: quantity * unitCost,
            Reasoning: $"{quantity} adet siparis onerilir. Tahmini maliyet {quantity * unitCost:N2} TL. " +
                       "Tedarik suresi 5-7 gun, mevcut stok 10 gun yeterli.");

        return Task.FromResult(suggestion);
    }
}
