using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI;

public sealed class MockPriceOptimizationService : IPriceOptimizationService
{
    private readonly ILogger<MockPriceOptimizationService> _logger;

    public MockPriceOptimizationService(ILogger<MockPriceOptimizationService> logger)
    {
        _logger = logger;
    }

    public Task<PriceOptimization> OptimizePriceAsync(
        Guid productId, decimal currentPrice, decimal costPrice,
        string platformCode, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] PriceOptimization.Optimize: productId={ProductId}, fiyat={Price}, maliyet={Cost}",
            productId, currentPrice, costPrice);

        var hash = Math.Abs(productId.GetHashCode());
        var targetMargin = 0.25m;
        var recommendedPrice = Math.Round(costPrice / (1 - targetMargin), 2);
        var margin = currentPrice > 0 ? (currentPrice - costPrice) / currentPrice : 0;

        PriceStrategy strategy;
        string reasoning;

        if (margin < 0.10m)
        {
            strategy = PriceStrategy.Clearance;
            recommendedPrice = Math.Round(costPrice * 1.10m, 2);
            reasoning = "Mevcut marj %10 altinda. Tasfiye fiyati onerilir veya maliyet gozden gecirilmeli.";
        }
        else if (margin > 0.40m)
        {
            strategy = PriceStrategy.MarginMaximize;
            reasoning = "Yuksek marj — rekabetci fiyat baskisi dusuk, mevcut fiyat korunabilir.";
        }
        else
        {
            strategy = hash % 2 == 0 ? PriceStrategy.Competitive : PriceStrategy.BuyboxWin;
            reasoning = strategy == PriceStrategy.BuyboxWin
                ? "Buybox kazanmak icin rakiplerin 1 TL altina inmek onerilir."
                : "Rekabetci fiyat — pazar ortalamasinin hafif altinda konumlanma.";
        }

        var result = new PriceOptimization(
            productId,
            $"SKU-{hash % 10000:D4}",
            currentPrice, costPrice,
            recommendedPrice,
            MinPrice: Math.Round(costPrice * 1.05m, 2),
            MaxPrice: Math.Round(costPrice * 1.60m, 2),
            ExpectedMargin: Math.Round((recommendedPrice - costPrice) / recommendedPrice * 100, 1),
            Confidence: 0.78,
            strategy, reasoning);

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<PriceOptimization>> OptimizeBulkAsync(
        Guid tenantId, string? platformCode = null,
        string? categoryId = null, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] PriceOptimization.Bulk: tenant={TenantId}, platform={Platform}",
            tenantId, platformCode);

        var items = new List<PriceOptimization>();
        var products = new[]
        {
            ("SKU-BULK-001", 89.90m, 35m),
            ("SKU-BULK-002", 449.90m, 180m),
            ("SKU-BULK-003", 299.90m, 120m)
        };

        foreach (var (sku, price, cost) in products)
        {
            var recommended = Math.Round(cost / 0.75m, 2);
            items.Add(new PriceOptimization(
                Guid.NewGuid(), sku, price, cost, recommended,
                Math.Round(cost * 1.05m, 2), Math.Round(cost * 1.60m, 2),
                Math.Round((recommended - cost) / recommended * 100, 1),
                0.80, PriceStrategy.Competitive,
                $"{sku}: Optimal fiyat {recommended:N2} TL (%25 marj hedefi)"));
        }

        return Task.FromResult<IReadOnlyList<PriceOptimization>>(items);
    }

    public Task<PriceHistory> GetPriceHistoryAsync(
        Guid productId, int days = 30, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] PriceOptimization.History: productId={ProductId}, gun={Days}",
            productId, days);

        var hash = Math.Abs(productId.GetHashCode());
        var basePrice = 100m + (hash % 200);

        var pricePoints = new List<PricePoint>();
        var aiPoints = new List<PricePoint>();

        for (int i = days; i >= 0; i--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            var variation = (hash + i) % 10 - 5;
            pricePoints.Add(new PricePoint(date, basePrice + variation, "platform"));

            if (i % 7 == 0)
            {
                aiPoints.Add(new PricePoint(date, basePrice + variation - 3, "ai.price.recommended"));
            }
        }

        var history = new PriceHistory(
            productId, $"SKU-{hash % 10000:D4}",
            pricePoints, aiPoints,
            pricePoints.Average(p => p.Price),
            aiPoints.Count > 0 ? aiPoints.Average(p => p.Price) : 0);

        return Task.FromResult(history);
    }
}
