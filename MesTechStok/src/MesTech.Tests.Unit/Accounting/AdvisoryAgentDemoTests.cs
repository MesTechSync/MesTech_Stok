using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Accounting;

/// <summary>
/// C-M4-04 — Advisory Agent Demo Output.
/// Simulates an advisory agent analyzing platform margins and producing
/// actionable recommendations for stock reallocation and pricing.
///
/// 5 products x 3 platforms (Trendyol, Hepsiburada, N11).
/// Commission rates: Trendyol %15, Hepsiburada %18, N11 %12 (static fallback).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "AdvisoryAgent")]
[Trait("Phase", "Dalga14")]
public class AdvisoryAgentDemoTests
{
    // ── Platform commission rates (percentage, domain rates) ──
    private static readonly Dictionary<string, decimal> PlatformCommissionRates = new()
    {
        ["Trendyol"] = 15m,      // %15
        ["Hepsiburada"] = 18m,   // %18
        ["N11"] = 12m            // %12
    };

    // ── 5 demo products ──
    private static readonly List<Product> DemoProducts = new()
    {
        CreateProduct("Bluetooth Kulaklik", "SKU-BT01", purchasePrice: 250m, salePrice: 500m),
        CreateProduct("USB-C Hub 7in1", "SKU-HUB02", purchasePrice: 180m, salePrice: 350m),
        CreateProduct("Mekanik Klavye", "SKU-KB03", purchasePrice: 400m, salePrice: 750m),
        CreateProduct("Webcam 1080p", "SKU-WC04", purchasePrice: 300m, salePrice: 320m),  // very thin margin
        CreateProduct("Monitor Standı", "SKU-MS05", purchasePrice: 120m, salePrice: 200m),
    };

    // ── PlatformCommission entities for each product x platform ──
    private static readonly List<PlatformCommission> Commissions = BuildCommissions();

    // ── Helper: margin analysis result ──
    private record PlatformMarginResult(
        string ProductName,
        string SKU,
        string Platform,
        decimal SalePrice,
        decimal PurchasePrice,
        decimal CommissionRate,
        decimal CommissionAmount,
        decimal NetRevenue,
        decimal NetMarginPercent);

    private record StockRecommendation(
        string ProductName,
        string SKU,
        string FromPlatform,
        string ToPlatform,
        decimal FromMargin,
        decimal ToMargin,
        decimal MarginDelta,
        string Recommendation);

    // ─────────────────────────────────────────────────────────────
    // Test 1: PlatformMarginAnalysis_HighestMarginIdentified
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void PlatformMarginAnalysis_HighestMarginIdentified()
    {
        // Arrange — analyze all products across all platforms
        var allMargins = CalculateAllMargins();

        // Act — for each product, find the platform with the highest net margin
        var bestPlatforms = allMargins
            .GroupBy(m => m.SKU)
            .Select(g => g.OrderByDescending(m => m.NetMarginPercent).First())
            .ToList();

        // Assert — N11 should have highest margins (lowest commission at %12)
        bestPlatforms.Should().HaveCount(5, "all 5 products must have a best platform");

        foreach (var best in bestPlatforms)
        {
            best.Platform.Should().Be("N11",
                $"{best.ProductName} should have highest margin on N11 (lowest commission %12)");
            best.NetMarginPercent.Should().BeGreaterThan(0,
                $"{best.ProductName} margin on best platform must be positive");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Test 2: PlatformMarginAnalysis_LowMarginWarning (margin < 5%)
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void PlatformMarginAnalysis_LowMarginWarning_DetectsSubFivePercentMargin()
    {
        // Arrange — Webcam 1080p has purchase 300, sale 320 => very thin
        var allMargins = CalculateAllMargins();
        const decimal lowMarginThreshold = 5m;

        // Act — find all product-platform combos with margin < 5%
        var lowMarginItems = allMargins
            .Where(m => m.NetMarginPercent < lowMarginThreshold)
            .ToList();

        var warnings = lowMarginItems
            .Select(m => $"UYARI: {m.ProductName} {m.Platform}'da net marj {m.NetMarginPercent:N2}% (< {lowMarginThreshold}%)")
            .ToList();

        // Assert — Webcam on Hepsiburada (%18 commission) should trigger warning
        lowMarginItems.Should().NotBeEmpty("at least one product-platform combo should have < 5% margin");

        var webcamHb = lowMarginItems
            .FirstOrDefault(m => m.SKU == "SKU-WC04" && m.Platform == "Hepsiburada");
        webcamHb.Should().NotBeNull("Webcam on Hepsiburada should be flagged as low margin");
        webcamHb!.NetMarginPercent.Should().BeLessThan(lowMarginThreshold);

        // Verify warning text is generated
        warnings.Should().Contain(w => w.Contains("Webcam 1080p") && w.Contains("Hepsiburada"));
    }

    // ─────────────────────────────────────────────────────────────
    // Test 3: StockRecommendation_ShiftToHigherMarginPlatform
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void StockRecommendation_ShiftToHigherMarginPlatform()
    {
        // Arrange
        var allMargins = CalculateAllMargins();

        // Act — for each product, compare worst vs best platform and generate recommendation
        var recommendations = new List<StockRecommendation>();

        foreach (var group in allMargins.GroupBy(m => m.SKU))
        {
            var ordered = group.OrderBy(m => m.NetMarginPercent).ToList();
            var worst = ordered.First();
            var best = ordered.Last();

            if (best.NetMarginPercent - worst.NetMarginPercent > 2m) // significant delta
            {
                var delta = best.NetMarginPercent - worst.NetMarginPercent;
                var recommendation = $"{worst.ProductName} has {worst.NetMarginPercent:N2}% margin on " +
                    $"{worst.Platform} but {best.NetMarginPercent:N2}% on {best.Platform} " +
                    $"→ shift stock to {best.Platform}";

                recommendations.Add(new StockRecommendation(
                    worst.ProductName,
                    worst.SKU,
                    worst.Platform,
                    best.Platform,
                    worst.NetMarginPercent,
                    best.NetMarginPercent,
                    delta,
                    recommendation));
            }
        }

        // Assert — all 5 products should have a shift recommendation (HB→N11)
        recommendations.Should().HaveCount(5, "all products benefit from shifting HB→N11");

        foreach (var rec in recommendations)
        {
            rec.FromPlatform.Should().Be("Hepsiburada", "worst platform is always HB (highest commission)");
            rec.ToPlatform.Should().Be("N11", "best platform is always N11 (lowest commission)");
            rec.MarginDelta.Should().BeGreaterThan(2m, "delta must be significant enough to recommend shift");
            rec.Recommendation.Should().Contain("shift stock to N11");
        }

        // Verify specific product: Bluetooth Kulaklik
        var btRec = recommendations.First(r => r.SKU == "SKU-BT01");
        btRec.Recommendation.Should().Contain("Bluetooth Kulaklik");
        btRec.ToMargin.Should().BeGreaterThan(btRec.FromMargin);
    }

    // ─────────────────────────────────────────────────────────────
    // Test 4: CommissionComparison_AllPlatforms
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void CommissionComparison_AllPlatforms_UsesRealPlatformCommissionEntity()
    {
        // Arrange — use PlatformCommission entities
        var testProduct = DemoProducts[0]; // Bluetooth Kulaklik, sale 500 TL
        var salePrice = testProduct.SalePrice;

        // Act — calculate commission for each platform using domain entity
        var results = Commissions
            .Where(c => c.CategoryName == "Elektronik")
            .GroupBy(c => c.Platform)
            .Select(g =>
            {
                var commission = g.First();
                var commissionAmount = commission.Calculate(salePrice);
                return new
                {
                    Platform = commission.Platform,
                    Rate = commission.Rate,
                    CommissionAmount = commissionAmount,
                    NetAfterCommission = salePrice - commissionAmount
                };
            })
            .OrderBy(r => r.Rate)
            .ToList();

        // Assert — 3 platforms, ordered by rate
        results.Should().HaveCount(3);

        // N11 lowest (12%), Trendyol middle (15%), Hepsiburada highest (18%)
        results[0].Platform.Should().Be(PlatformType.N11);
        results[0].Rate.Should().Be(12m);
        results[0].CommissionAmount.Should().Be(60m); // 500 * 12 / 100

        results[1].Platform.Should().Be(PlatformType.Trendyol);
        results[1].Rate.Should().Be(15m);
        results[1].CommissionAmount.Should().Be(75m); // 500 * 15 / 100

        results[2].Platform.Should().Be(PlatformType.Hepsiburada);
        results[2].Rate.Should().Be(18m);
        results[2].CommissionAmount.Should().Be(90m); // 500 * 18 / 100

        // Net revenue ordering: N11 > Trendyol > HB
        results[0].NetAfterCommission.Should().BeGreaterThan(results[1].NetAfterCommission);
        results[1].NetAfterCommission.Should().BeGreaterThan(results[2].NetAfterCommission);
    }

    // ─────────────────────────────────────────────────────────────
    // Test 5: ProfitOptimization_SuggestPriceIncrease
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void ProfitOptimization_SuggestPriceIncrease_WhenMarginBelowTarget()
    {
        // Arrange — target net margin: 15%
        const decimal targetMarginPercent = 15m;
        var allMargins = CalculateAllMargins();

        // Act — for products below target on any platform, suggest minimum price increase
        var priceIncreases = new List<(string ProductName, string SKU, string Platform,
            decimal CurrentPrice, decimal SuggestedPrice, decimal CurrentMargin, decimal NewMargin)>();

        foreach (var margin in allMargins.Where(m => m.NetMarginPercent < targetMarginPercent))
        {
            // Calculate required sale price for target margin:
            // NetMargin = (SalePrice - PurchasePrice - Commission) / SalePrice * 100
            // TargetMargin / 100 = (SP - PP - SP * CR/100) / SP
            // TargetMargin / 100 = 1 - PP/SP - CR/100
            // PP/SP = 1 - CR/100 - TargetMargin/100
            // SP = PP / (1 - CR/100 - TargetMargin/100)
            var commissionDecimal = margin.CommissionRate / 100m;
            var targetDecimal = targetMarginPercent / 100m;
            var denominator = 1m - commissionDecimal - targetDecimal;

            if (denominator <= 0)
                continue; // mathematically impossible to achieve target

            var suggestedPrice = Math.Ceiling(margin.PurchasePrice / denominator);
            var newCommission = suggestedPrice * commissionDecimal;
            var newNetRevenue = suggestedPrice - margin.PurchasePrice - newCommission;
            var newMargin = suggestedPrice > 0 ? newNetRevenue / suggestedPrice * 100m : 0m;

            priceIncreases.Add((
                margin.ProductName,
                margin.SKU,
                margin.Platform,
                margin.SalePrice,
                suggestedPrice,
                margin.NetMarginPercent,
                Math.Round(newMargin, 2)));
        }

        // Assert — Webcam (thin margin) should have price increase suggestions
        priceIncreases.Should().NotBeEmpty("some products are below 15% target margin");

        var webcamSuggestions = priceIncreases.Where(p => p.SKU == "SKU-WC04").ToList();
        webcamSuggestions.Should().NotBeEmpty("Webcam has very thin margins across all platforms");

        foreach (var suggestion in webcamSuggestions)
        {
            suggestion.SuggestedPrice.Should().BeGreaterThan(suggestion.CurrentPrice,
                $"price must increase on {suggestion.Platform} to reach {targetMarginPercent}% margin");
            suggestion.NewMargin.Should().BeGreaterThanOrEqualTo(targetMarginPercent,
                "new price must achieve at least target margin");
        }

        // Monitor Standı on Hepsiburada: purchase 120, sale 200, commission %18
        // Margin = (200 - 120 - 36) / 200 * 100 = 22% — above target, should NOT appear
        var monitorHb = priceIncreases.FirstOrDefault(p => p.SKU == "SKU-MS05" && p.Platform == "Hepsiburada");

        // Monitor Standı: (200 - 120 - 36) / 200 * 100 = 22% → above 15%, shouldn't appear
        // But let's verify it depending on actual calculation
        var monitorMargin = allMargins.First(m => m.SKU == "SKU-MS05" && m.Platform == "Hepsiburada");
        if (monitorMargin.NetMarginPercent >= targetMarginPercent)
        {
            monitorHb.Should().BeNull("Monitor Standı on HB is above target margin");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Test 6: DetailedProfitService_IntegrationWithAdvisory
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void DetailedProfitService_IntegrationWithAdvisory_ProducesCorrectPnL()
    {
        // Arrange — use ProfitCalculationService for per-platform P&L
        var profitService = new ProfitCalculationService();

        // Simulate: Bluetooth Kulaklik, 10 units sold on Trendyol
        var quantity = 10;
        var product = DemoProducts[0]; // purchase 250, sale 500
        var totalRevenue = product.SalePrice * quantity; // 5000
        var totalCogs = product.PurchasePrice * quantity; // 2500
        var commissionRate = 0.15m; // Trendyol
        var totalCommission = totalRevenue * commissionRate; // 750
        var totalCargo = 15m * quantity; // 150 TL cargo
        var totalWithholding = 0m;
        var otherExpenses = 0m;

        // Act
        var result = profitService.CalculateDetailed(
            totalRevenue, totalCogs, totalCommission, totalCargo, totalWithholding, otherExpenses);

        // Assert
        result.TotalRevenue.Should().Be(5000m);
        result.TotalCogs.Should().Be(2500m);
        result.GrossProfit.Should().Be(2500m); // 5000 - 2500
        result.TotalCommission.Should().Be(750m);
        result.NetProfit.Should().Be(1600m); // 2500 - 750 - 150
        result.NetMargin.Should().Be(32m); // 1600/5000 * 100
    }

    // ─────────────────────────────────────────────────────────────
    // Test 7: CommissionRecord_NetAmount_MatchesMarginAnalysis
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public void CommissionRecord_NetAmount_MatchesMarginAnalysis()
    {
        // Arrange — create CommissionRecord for USB-C Hub on N11
        var product = DemoProducts[1]; // USB-C Hub: purchase 180, sale 350
        var tenantId = Guid.NewGuid();
        var commissionRate = 0.12m; // N11
        var grossAmount = product.SalePrice;
        var commissionAmount = Math.Round(grossAmount * commissionRate, 2);
        var serviceFee = 5m; // platform service fee

        var record = CommissionRecord.Create(
            tenantId,
            platform: "N11",
            grossAmount: grossAmount,
            commissionRate: commissionRate,
            commissionAmount: commissionAmount,
            serviceFee: serviceFee,
            orderId: "ORD-2026-001",
            category: "Elektronik",
            commissionType: CommissionType.Percentage,
            rateSource: "StaticFallback");

        // Act
        var netAmount = record.GetNetAmount();
        var expectedNet = grossAmount - commissionAmount - serviceFee;

        // Assert
        netAmount.Should().Be(expectedNet);
        record.CommissionRate.Should().Be(commissionRate);
        record.Platform.Should().Be("N11");
        record.CommissionAmount.Should().Be(42m); // 350 * 0.12
        netAmount.Should().Be(303m); // 350 - 42 - 5
    }

    // ════════════════════════════════════════════════════════════
    //  Helper methods
    // ════════════════════════════════════════════════════════════

    private static Product CreateProduct(string name, string sku, decimal purchasePrice, decimal salePrice)
    {
        return new Product
        {
            Name = name,
            SKU = sku,
            PurchasePrice = purchasePrice,
            SalePrice = salePrice,
            Stock = 100,
            CategoryId = Guid.NewGuid(),
            TaxRate = 0.18m,
            CurrencyCode = "TRY",
            IsActive = true
        };
    }

    private static List<PlatformCommission> BuildCommissions()
    {
        var commissions = new List<PlatformCommission>();
        var tenantId = Guid.NewGuid();

        foreach (var (platformName, rate) in PlatformCommissionRates)
        {
            var platformType = platformName switch
            {
                "Trendyol" => PlatformType.Trendyol,
                "Hepsiburada" => PlatformType.Hepsiburada,
                "N11" => PlatformType.N11,
                _ => PlatformType.Trendyol
            };

            commissions.Add(new PlatformCommission
            {
                TenantId = tenantId,
                Platform = platformType,
                Type = CommissionType.Percentage,
                CategoryName = "Elektronik",
                Rate = rate,
                Currency = "TRY",
                EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            });
        }

        return commissions;
    }

    /// <summary>
    /// Calculates net margin for every product x platform combination.
    /// Net margin = (SalePrice - PurchasePrice - CommissionAmount) / SalePrice * 100
    /// </summary>
    private static List<PlatformMarginResult> CalculateAllMargins()
    {
        var results = new List<PlatformMarginResult>();

        foreach (var product in DemoProducts)
        {
            foreach (var (platformName, commissionRatePercent) in PlatformCommissionRates)
            {
                var commissionAmount = Math.Round(product.SalePrice * commissionRatePercent / 100m, 2);
                var netRevenue = product.SalePrice - product.PurchasePrice - commissionAmount;
                var netMarginPercent = product.SalePrice > 0
                    ? Math.Round(netRevenue / product.SalePrice * 100m, 2)
                    : 0m;

                results.Add(new PlatformMarginResult(
                    product.Name,
                    product.SKU,
                    platformName,
                    product.SalePrice,
                    product.PurchasePrice,
                    commissionRatePercent,
                    commissionAmount,
                    netRevenue,
                    netMarginPercent));
            }
        }

        return results;
    }
}
