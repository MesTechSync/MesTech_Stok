using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI;

public sealed class MockBuyboxService : IBuyboxService
{
    private readonly ILogger<MockBuyboxService> _logger;

    private static readonly string[] CompetitorNames =
    {
        "TeknoMarket", "SanalPazar", "HizliTicaret", "UcuzBul", "MegaShop"
    };

    public MockBuyboxService(ILogger<MockBuyboxService> logger)
    {
        _logger = logger;
    }

    public Task<BuyboxAnalysis> AnalyzeCompetitorsAsync(
        string sku, decimal currentPrice, string platformCode,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] BuyboxService.AnalyzeCompetitors: SKU={SKU}, fiyat={Price}, platform={Platform}",
            sku, currentPrice, platformCode);

        var hash = Math.Abs(sku.GetHashCode());
        var hasBuybox = hash % 10 > 3; // %70 buybox var

        var competitors = new List<CompetitorPriceInfo>();
        var competitorCount = (hash % 3) + 2; // 2-4 rakip

        for (int i = 0; i < competitorCount; i++)
        {
            var priceVariation = 1.0m + ((hash + i * 7) % 30 - 15) / 100.0m;
            var competitorPrice = Math.Round(currentPrice * priceVariation, 2);
            competitors.Add(new CompetitorPriceInfo(
                CompetitorNames[(hash + i) % CompetitorNames.Length],
                competitorPrice,
                ShippingCost: (hash + i) % 3 == 0 ? 0 : 14.99m,
                SellerRating: 70 + ((hash + i) % 30),
                HasBuybox: i == 0 && !hasBuybox,
                LastChecked: DateTime.UtcNow.AddMinutes(-(hash % 60))));
        }

        var lowestCompetitor = competitors.OrderBy(c => c.Price).First();
        var suggestedPrice = hasBuybox
            ? currentPrice
            : Math.Round(lowestCompetitor.Price - 1m, 2);

        var result = new BuyboxAnalysis(
            hasBuybox, currentPrice,
            lowestCompetitor.Price, lowestCompetitor.SellerName,
            competitors, suggestedPrice,
            hasBuybox
                ? "Buybox sizde, mevcut fiyat uygun."
                : $"Buybox {lowestCompetitor.SellerName}'da. {suggestedPrice:N2} TL ile geri kazanilabilir.");

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<BuyboxPosition>> CheckBuyboxPositionsAsync(
        Guid tenantId, string platformCode, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] BuyboxService.CheckPositions: tenant={TenantId}, platform={Platform}",
            tenantId, platformCode);

        var positions = new List<BuyboxPosition>();
        var skus = new[] { "SKU-MOCK-001", "SKU-MOCK-002", "SKU-MOCK-003", "SKU-MOCK-004", "SKU-MOCK-005" };

        for (int i = 0; i < skus.Length; i++)
        {
            var price = 100m + i * 50m;
            var buyboxPrice = price + (i % 2 == 0 ? -5m : 3m);
            positions.Add(new BuyboxPosition(
                Guid.NewGuid(), skus[i],
                HasBuybox: i % 2 == 0,
                price, buyboxPrice,
                Math.Abs(price - buyboxPrice)));
        }

        return Task.FromResult<IReadOnlyList<BuyboxPosition>>(positions);
    }

    public Task<IReadOnlyList<BuyboxLostItem>> GetLostBuyboxesAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] BuyboxService.GetLostBuyboxes: tenant={TenantId}", tenantId);

        var items = new List<BuyboxLostItem>
        {
            new(Guid.NewGuid(), "SKU-LOST-001", "Samsung Galaxy A54 Kilif",
                149.90m, 139.90m, "TeknoMarket", DateTime.UtcNow.AddHours(-2)),
            new(Guid.NewGuid(), "SKU-LOST-002", "iPhone 15 Ekran Koruyucu",
                79.90m, 69.90m, "SanalPazar", DateTime.UtcNow.AddHours(-5))
        };

        return Task.FromResult<IReadOnlyList<BuyboxLostItem>>(items);
    }
}
