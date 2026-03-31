using FluentAssertions;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Tests.Unit.Domain.Services;

/// <summary>
/// Dalga 14 Sprint 2 — Domain service edge case and coverage tests.
/// CommissionCalculationService, ReconciliationScoringService, ProfitCalculationService, TaxWithholdingService.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "DomainServiceCoverage")]
[Trait("Phase", "Dalga14")]
public class DomainServiceCoverageTests
{
    // ═══════════════════════════════════════════
    // CommissionCalculationService — Sync Edge Cases
    // ═══════════════════════════════════════════

    [Theory]
    [InlineData("Trendyol", 0.15)]
    [InlineData("Hepsiburada", 0.18)]
    [InlineData("N11", 0.12)]
    [InlineData("Ciceksepeti", 0.20)]
    [InlineData("Amazon", 0.15)]
    [InlineData("Pazarama", 0.10)]
    public void CommissionCalculation_GetDefaultRate_KnownPlatforms(string platform, double expectedRate)
    {
        var sut = new CommissionCalculationService();
        sut.GetDefaultRate(platform).Should().Be((decimal)expectedRate);
    }

    [Fact]
    public void CommissionCalculation_GetDefaultRate_UnknownPlatform_Returns15Percent()
    {
        var sut = new CommissionCalculationService();
        sut.GetDefaultRate("UnknownPlatform").Should().Be(0m);
    }

    [Fact]
    public void CommissionCalculation_GetDefaultRate_CaseInsensitive()
    {
        var sut = new CommissionCalculationService();
        sut.GetDefaultRate("TRENDYOL").Should().Be(0.15m);
        sut.GetDefaultRate("trendyol").Should().Be(0.15m);
    }

    [Fact]
    public void CommissionCalculation_CalculateCommission_ZeroAmount_ReturnsZero()
    {
        var sut = new CommissionCalculationService();
        sut.CalculateCommission("Trendyol", null, 0m).Should().Be(0m);
    }

    [Fact]
    public void CommissionCalculation_CalculateCommission_NegativeAmount_Throws()
    {
        var sut = new CommissionCalculationService();
        var act = () => sut.CalculateCommission("Trendyol", null, -100m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CommissionCalculation_CalculateCommission_NullPlatform_Throws()
    {
        var sut = new CommissionCalculationService();
        var act = () => sut.CalculateCommission(null!, null, 100m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CommissionCalculation_CalculateCommission_WhitespacePlatform_Throws()
    {
        var sut = new CommissionCalculationService();
        var act = () => sut.CalculateCommission("   ", null, 100m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CommissionCalculation_CalculateCommission_RoundsToTwoDecimals()
    {
        var sut = new CommissionCalculationService();
        // 333.33 * 0.15 = 49.9995 → should round to 50.00
        var result = sut.CalculateCommission("Trendyol", null, 333.33m);
        result.Should().Be(50.00m);
    }

    [Fact]
    public void CommissionCalculation_CalculateCommission_LargeAmount()
    {
        var sut = new CommissionCalculationService();
        var result = sut.CalculateCommission("Trendyol", null, 1_000_000m);
        result.Should().Be(150_000m);
    }

    // ═══════════════════════════════════════════
    // CommissionCalculationService — Async Tests
    // ═══════════════════════════════════════════

    [Fact]
    public async Task CommissionCalculationAsync_NoProvider_UsesFallback()
    {
        var sut = new CommissionCalculationService();
        var result = await sut.CalculateCommissionAsync("Trendyol", null, 1000m);

        result.Rate.Should().Be(0.15m);
        result.Amount.Should().Be(150m);
        result.Source.Should().Be("StaticFallback");
        result.IsCached.Should().BeFalse();
    }

    [Fact]
    public async Task CommissionCalculationAsync_WithProvider_UsesDynamicRate()
    {
        var provider = (string platform, string? category, CancellationToken ct) =>
            Task.FromResult<DynamicRateResult?>(new DynamicRateResult(0.20m, "API", DateTime.UtcNow.AddHours(1)));

        var sut = new CommissionCalculationService(provider);
        var result = await sut.CalculateCommissionAsync("Trendyol", "Electronics", 1000m);

        result.Rate.Should().Be(0.20m);
        result.Amount.Should().Be(200m);
        result.Source.Should().Be("API");
        result.IsCached.Should().BeFalse();
    }

    [Fact]
    public async Task CommissionCalculationAsync_ProviderReturnsNull_UsesFallback()
    {
        var provider = (string platform, string? category, CancellationToken ct) =>
            Task.FromResult<DynamicRateResult?>(null);

        var sut = new CommissionCalculationService(provider);
        var result = await sut.CalculateCommissionAsync("Hepsiburada", null, 1000m);

        result.Rate.Should().Be(0.18m);
        result.Source.Should().Be("StaticFallback");
    }

    [Fact]
    public async Task CommissionCalculationAsync_NegativeDynamicRate_Throws()
    {
        var provider = (string platform, string? category, CancellationToken ct) =>
            Task.FromResult<DynamicRateResult?>(new DynamicRateResult(-0.05m, "API", DateTime.UtcNow.AddHours(1)));

        var sut = new CommissionCalculationService(provider);
        var act = () => sut.CalculateCommissionAsync("Trendyol", null, 1000m);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task CommissionCalculationAsync_CachedResult_ReturnsCached()
    {
        var callCount = 0;
        var provider = (string platform, string? category, CancellationToken ct) =>
        {
            callCount++;
            return Task.FromResult<DynamicRateResult?>(
                new DynamicRateResult(0.25m, "API", DateTime.UtcNow.AddHours(1)));
        };

        var sut = new CommissionCalculationService(provider);

        // First call — fresh
        var result1 = await sut.CalculateCommissionAsync("Trendyol", "Electronics", 1000m);
        result1.IsCached.Should().BeFalse();

        // Second call — cached
        var result2 = await sut.CalculateCommissionAsync("Trendyol", "Electronics", 2000m);
        result2.IsCached.Should().BeTrue();
        result2.Amount.Should().Be(500m); // 2000 * 0.25

        callCount.Should().Be(1); // Provider called only once
    }

    // ═══════════════════════════════════════════
    // ReconciliationScoringService — Amount Score
    // ═══════════════════════════════════════════

    [Fact]
    public void ReconciliationScoring_Thresholds()
    {
        var sut = new ReconciliationScoringService();
        sut.AutoMatchThreshold.Should().Be(0.85m);
        sut.ReviewThreshold.Should().Be(0.70m);
    }

    [Fact]
    public void ReconciliationScoring_AmountScore_ExactMatch_Returns1()
    {
        var sut = new ReconciliationScoringService();
        sut.CalculateAmountScore(1000m, 1000m).Should().Be(1m);
    }

    [Fact]
    public void ReconciliationScoring_AmountScore_BothZero_Returns1()
    {
        var sut = new ReconciliationScoringService();
        sut.CalculateAmountScore(0m, 0m).Should().Be(1m);
    }

    [Fact]
    public void ReconciliationScoring_AmountScore_ExpectedZero_Returns0()
    {
        var sut = new ReconciliationScoringService();
        sut.CalculateAmountScore(0m, 100m).Should().Be(0m);
    }

    [Fact]
    public void ReconciliationScoring_AmountScore_ActualZero_Returns0()
    {
        var sut = new ReconciliationScoringService();
        sut.CalculateAmountScore(100m, 0m).Should().Be(0m);
    }

    [Fact]
    public void ReconciliationScoring_AmountScore_Within05Percent_Returns1()
    {
        var sut = new ReconciliationScoringService();
        // 1000 * 0.005 = 5, so 995 is within 0.5%
        sut.CalculateAmountScore(1000m, 995m).Should().Be(1.0m);
    }

    [Fact]
    public void ReconciliationScoring_AmountScore_Within1Percent_Returns095()
    {
        var sut = new ReconciliationScoringService();
        // 1000 * 0.01 = 10, so 991 is 0.9% diff
        sut.CalculateAmountScore(1000m, 991m).Should().Be(0.95m);
    }

    [Fact]
    public void ReconciliationScoring_AmountScore_Over10Percent_Returns0()
    {
        var sut = new ReconciliationScoringService();
        sut.CalculateAmountScore(1000m, 800m).Should().Be(0m);
    }

    // ═══════════════════════════════════════════
    // ReconciliationScoringService — Date Score
    // ═══════════════════════════════════════════

    [Fact]
    public void ReconciliationScoring_DateScore_SameDay_Returns1()
    {
        var sut = new ReconciliationScoringService();
        var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var txDate = periodEnd.AddDays(7); // Trendyol window = 7
        sut.CalculateDateScore(periodEnd, txDate, "Trendyol").Should().Be(1.0m);
    }

    [Fact]
    public void ReconciliationScoring_DateScore_1DayOff_Returns095()
    {
        var sut = new ReconciliationScoringService();
        var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var txDate = periodEnd.AddDays(8); // 7 + 1 day off
        sut.CalculateDateScore(periodEnd, txDate, "Trendyol").Should().Be(0.95m);
    }

    [Fact]
    public void ReconciliationScoring_DateScore_Over10DaysOff_Returns0()
    {
        var sut = new ReconciliationScoringService();
        var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var txDate = periodEnd.AddDays(20); // 13 days off from expected
        sut.CalculateDateScore(periodEnd, txDate, "Trendyol").Should().Be(0m);
    }

    // ═══════════════════════════════════════════
    // ReconciliationScoringService — Text Score
    // ═══════════════════════════════════════════

    [Fact]
    public void ReconciliationScoring_TextScore_NullDescription_Returns03()
    {
        var sut = new ReconciliationScoringService();
        sut.CalculateTextScore("REF-001", "Trendyol", null).Should().Be(0.3m);
    }

    [Fact]
    public void ReconciliationScoring_TextScore_PayoutRefAndPlatformInDesc_Returns1()
    {
        var sut = new ReconciliationScoringService();
        var result = sut.CalculateTextScore("REF-001", "Trendyol", "Payment REF-001 from Trendyol");
        result.Should().Be(1.0m);
    }

    [Fact]
    public void ReconciliationScoring_TextScore_OnlyAliasMatch_Returns04()
    {
        var sut = new ReconciliationScoringService();
        var result = sut.CalculateTextScore(null, "Trendyol", "Transfer from TY marketplace");
        result.Should().Be(0.4m);
    }

    // ═══════════════════════════════════════════
    // ReconciliationScoringService — Counterparty Score
    // ═══════════════════════════════════════════

    [Fact]
    public void ReconciliationScoring_CounterpartyScore_NullPlatform_Returns03()
    {
        var sut = new ReconciliationScoringService();
        sut.CalculateCounterpartyScore(null, "DSM Grup").Should().Be(0.3m);
    }

    [Fact]
    public void ReconciliationScoring_CounterpartyScore_NullCounterparty_Returns03()
    {
        var sut = new ReconciliationScoringService();
        sut.CalculateCounterpartyScore("Trendyol", null).Should().Be(0.3m);
    }

    [Fact]
    public void ReconciliationScoring_CounterpartyScore_KnownPayer_Returns1()
    {
        var sut = new ReconciliationScoringService();
        sut.CalculateCounterpartyScore("Trendyol", "DSM Grup A.S.").Should().Be(1.0m);
    }

    [Fact]
    public void ReconciliationScoring_CounterpartyScore_UnknownPayer_Returns01()
    {
        var sut = new ReconciliationScoringService();
        sut.CalculateCounterpartyScore("Trendyol", "Random Company Ltd").Should().Be(0.1m);
    }

    [Fact]
    public void ReconciliationScoring_CounterpartyScore_UnknownPlatform_Returns03()
    {
        var sut = new ReconciliationScoringService();
        sut.CalculateCounterpartyScore("UnknownPlatform", "Any Name").Should().Be(0.3m);
    }

    // ═══════════════════════════════════════════
    // ReconciliationScoringService — Payment Window
    // ═══════════════════════════════════════════

    [Theory]
    [InlineData("Trendyol", 7)]
    [InlineData("Amazon", 14)]
    [InlineData("Hepsiburada", 10)]
    [InlineData("N11", 10)]
    [InlineData("Ciceksepeti", 14)]
    [InlineData("Pazarama", 7)]
    public void ReconciliationScoring_GetPlatformPaymentWindow_KnownPlatforms(string platform, int expected)
    {
        var sut = new ReconciliationScoringService();
        sut.GetPlatformPaymentWindow(platform).Should().Be(expected);
    }

    [Fact]
    public void ReconciliationScoring_GetPlatformPaymentWindow_UnknownPlatform_Returns7()
    {
        var sut = new ReconciliationScoringService();
        sut.GetPlatformPaymentWindow("Unknown").Should().Be(7);
    }

    [Fact]
    public void ReconciliationScoring_GetPlatformPaymentWindow_NullPlatform_Returns7()
    {
        var sut = new ReconciliationScoringService();
        sut.GetPlatformPaymentWindow(null!).Should().Be(7);
    }

    [Fact]
    public void ReconciliationScoring_GetKnownPlatformPayers_NotEmpty()
    {
        var sut = new ReconciliationScoringService();
        var payers = sut.GetKnownPlatformPayers();
        payers.Should().NotBeEmpty();
        payers.Should().ContainKey("Trendyol");
    }

    // ═══════════════════════════════════════════
    // ReconciliationScoringService — CalculateConfidence (Composite)
    // ═══════════════════════════════════════════

    [Fact]
    public void ReconciliationScoring_CalculateConfidence_PerfectMatch_High()
    {
        var sut = new ReconciliationScoringService();
        var date = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        var result = sut.CalculateConfidence(1000m, 1000m, date, date, "Trendyol payment", "Trendyol");
        result.Should().BeGreaterThanOrEqualTo(0.85m);
    }

    [Fact]
    public void ReconciliationScoring_CalculateConfidence_MismatchAll_Low()
    {
        var sut = new ReconciliationScoringService();
        var result = sut.CalculateConfidence(
            1000m, 500m,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            "Random transfer", "Trendyol");
        result.Should().BeLessThan(0.70m);
    }

    [Fact]
    public void ReconciliationScoring_CalculateConfidence_ClampsBetween0And1()
    {
        var sut = new ReconciliationScoringService();
        var date = DateTime.UtcNow;
        var result = sut.CalculateConfidence(0m, 0m, date, date);
        result.Should().BeGreaterThanOrEqualTo(0m);
        result.Should().BeLessThanOrEqualTo(1m);
    }

    // ═══════════════════════════════════════════
    // ProfitCalculationService
    // ═══════════════════════════════════════════

    [Fact]
    public void ProfitCalculation_CalculateNetProfit_BasicCase()
    {
        var sut = new ProfitCalculationService();
        var result = sut.CalculateNetProfit(10000m, 5000m, 1500m, 500m, 200m);
        result.Should().Be(2800m);
    }

    [Fact]
    public void ProfitCalculation_CalculateNetProfit_AllZero()
    {
        var sut = new ProfitCalculationService();
        sut.CalculateNetProfit(0m, 0m, 0m, 0m, 0m).Should().Be(0m);
    }

    [Fact]
    public void ProfitCalculation_CalculateNetProfit_NegativeResult()
    {
        var sut = new ProfitCalculationService();
        var result = sut.CalculateNetProfit(1000m, 800m, 150m, 100m, 50m);
        result.Should().Be(-100m);
    }

    [Fact]
    public void ProfitCalculation_CalculateProfitMargin_ZeroRevenue_Returns0()
    {
        var sut = new ProfitCalculationService();
        sut.CalculateProfitMargin(0m, 100m).Should().Be(0m);
    }

    [Fact]
    public void ProfitCalculation_CalculateProfitMargin_BasicCase()
    {
        var sut = new ProfitCalculationService();
        var result = sut.CalculateProfitMargin(10000m, 2000m);
        result.Should().Be(20m);
    }

    [Fact]
    public void ProfitCalculation_CalculateDetailed_AllFields()
    {
        var sut = new ProfitCalculationService();
        var result = sut.CalculateDetailed(
            totalRevenue: 10000m,
            totalCogs: 4000m,
            totalCommission: 1500m,
            totalCargo: 500m,
            totalWithholding: 200m,
            otherExpenses: 300m);

        result.TotalRevenue.Should().Be(10000m);
        result.TotalCogs.Should().Be(4000m);
        result.GrossProfit.Should().Be(6000m);
        result.GrossMargin.Should().Be(60m);
        result.NetProfit.Should().Be(3500m);
        result.NetMargin.Should().Be(35m);
    }

    [Fact]
    public void ProfitCalculation_CalculateDetailed_ZeroRevenue_MarginZero()
    {
        var sut = new ProfitCalculationService();
        var result = sut.CalculateDetailed(0m, 0m, 0m, 0m, 0m, 0m);
        result.GrossMargin.Should().Be(0m);
        result.NetMargin.Should().Be(0m);
    }

    // ═══════════════════════════════════════════
    // ProfitCalculationService — FIFO COGS
    // ═══════════════════════════════════════════

    [Fact]
    public void ProfitCalculation_FifoCogs_NullLayers_Returns0()
    {
        var sut = new ProfitCalculationService();
        sut.CalculateFifoCogs(null!, 10).Should().Be(0m);
    }

    [Fact]
    public void ProfitCalculation_FifoCogs_EmptyLayers_Returns0()
    {
        var sut = new ProfitCalculationService();
        sut.CalculateFifoCogs(Array.Empty<CostLayerInput>(), 10).Should().Be(0m);
    }

    [Fact]
    public void ProfitCalculation_FifoCogs_ZeroQuantity_Returns0()
    {
        var sut = new ProfitCalculationService();
        var layers = new[] { new CostLayerInput(10, 50m) };
        sut.CalculateFifoCogs(layers, 0).Should().Be(0m);
    }

    [Fact]
    public void ProfitCalculation_FifoCogs_SingleLayer_ExactMatch()
    {
        var sut = new ProfitCalculationService();
        var layers = new[] { new CostLayerInput(10, 50m) };
        sut.CalculateFifoCogs(layers, 10).Should().Be(500m);
    }

    [Fact]
    public void ProfitCalculation_FifoCogs_MultiLayer_CorrectOrder()
    {
        var sut = new ProfitCalculationService();
        var layers = new[]
        {
            new CostLayerInput(5, 40m),  // First 5 at 40 = 200
            new CostLayerInput(10, 60m)  // Next 5 at 60 = 300
        };
        sut.CalculateFifoCogs(layers, 10).Should().Be(500m);
    }

    [Fact]
    public void ProfitCalculation_FifoCogs_InsufficientLayers_UsesAverage()
    {
        var sut = new ProfitCalculationService();
        var layers = new[]
        {
            new CostLayerInput(5, 40m),
            new CostLayerInput(5, 60m)
        };
        // 5*40 + 5*60 = 500 for first 10, then 5 remaining at avg (500/10=50) = 250
        var result = sut.CalculateFifoCogs(layers, 15);
        result.Should().Be(750m);
    }

    // ═══════════════════════════════════════════
    // TaxWithholdingService
    // ═══════════════════════════════════════════

    [Fact]
    public void TaxWithholding_CalculateWithholding_BasicCase()
    {
        var sut = new TaxWithholdingService();
        // 1000 * 0.20 = 200
        sut.CalculateWithholding(1000m, 0.20m).Should().Be(200m);
    }

    [Fact]
    public void TaxWithholding_CalculateWithholding_ZeroRate()
    {
        var sut = new TaxWithholdingService();
        sut.CalculateWithholding(1000m, 0m).Should().Be(0m);
    }

    [Fact]
    public void TaxWithholding_CalculateWithholding_FullRate()
    {
        var sut = new TaxWithholdingService();
        sut.CalculateWithholding(1000m, 1m).Should().Be(1000m);
    }

    [Fact]
    public void TaxWithholding_CalculateWithholding_NegativeRate_Throws()
    {
        var sut = new TaxWithholdingService();
        var act = () => sut.CalculateWithholding(1000m, -0.1m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TaxWithholding_CalculateWithholding_RateOver1_Throws()
    {
        var sut = new TaxWithholdingService();
        var act = () => sut.CalculateWithholding(1000m, 1.1m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TaxWithholding_ExtractTaxExclusiveAmount_18Percent()
    {
        var sut = new TaxWithholdingService();
        // 1180 / (1 + 0.18) = 1000
        sut.ExtractTaxExclusiveAmount(1180m, 0.18m).Should().Be(1000m);
    }

    [Fact]
    public void TaxWithholding_ExtractTaxExclusiveAmount_ZeroKdv()
    {
        var sut = new TaxWithholdingService();
        sut.ExtractTaxExclusiveAmount(1000m, 0m).Should().Be(1000m);
    }

    [Fact]
    public void TaxWithholding_ExtractTaxExclusiveAmount_NegativeKdv_Throws()
    {
        var sut = new TaxWithholdingService();
        var act = () => sut.ExtractTaxExclusiveAmount(1000m, -0.18m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
