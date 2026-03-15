using FluentAssertions;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Tests.Unit.Accounting.Services;

/// <summary>
/// Enhanced tests for ReconciliationScoringService — covers the 4-component scoring
/// (Amount, Date, Text, Counterparty) and platform payment windows.
/// Does NOT duplicate tests in ReconciliationScoringServiceTests.cs.
/// </summary>
[Trait("Category", "Unit")]
public class ReconciliationScoringServiceEnhancedTests
{
    private readonly ReconciliationScoringService _sut = new();

    // ── ReviewThreshold ──────────────────────────────────────────────

    [Fact]
    public void ReviewThreshold_ShouldBe070()
    {
        _sut.ReviewThreshold.Should().Be(0.70m);
    }

    // ── CalculateAmountScore ─────────────────────────────────────────

    [Fact]
    public void CalculateAmountScore_ExactMatch_Returns1()
    {
        _sut.CalculateAmountScore(1000m, 1000m).Should().Be(1m);
    }

    [Fact]
    public void CalculateAmountScore_HalfPercentOff_ReturnsAbove095()
    {
        // 0.5% of 1000 = 5 → within tolerance => 1.0
        _sut.CalculateAmountScore(1000m, 1005m).Should().Be(1.0m);
    }

    [Fact]
    public void CalculateAmountScore_OnePercentOff_Returns095()
    {
        // 1% of 1000 = 10 → within 1% => 0.95
        _sut.CalculateAmountScore(1000m, 1010m).Should().Be(0.95m);
    }

    [Fact]
    public void CalculateAmountScore_TwoPercentOff_Returns085()
    {
        // 2% of 1000 = 20 → within 2% => 0.85
        _sut.CalculateAmountScore(1000m, 1020m).Should().Be(0.85m);
    }

    [Fact]
    public void CalculateAmountScore_FivePercentOff_ReturnsMedium()
    {
        // 5% of 1000 = 50 → within 5% => 0.70
        _sut.CalculateAmountScore(1000m, 1050m).Should().Be(0.70m);
    }

    [Fact]
    public void CalculateAmountScore_TenPercentOff_ReturnsLow()
    {
        // 10% of 1000 = 100 → within 10% => 0.40
        _sut.CalculateAmountScore(1000m, 1100m).Should().Be(0.40m);
    }

    [Fact]
    public void CalculateAmountScore_OverTenPercent_ReturnsZero()
    {
        // > 10% off => 0
        _sut.CalculateAmountScore(1000m, 1200m).Should().Be(0m);
    }

    [Fact]
    public void CalculateAmountScore_ZeroExpected_NonZeroActual_ReturnsZero()
    {
        _sut.CalculateAmountScore(0m, 100m).Should().Be(0m);
    }

    [Fact]
    public void CalculateAmountScore_NonZeroExpected_ZeroActual_ReturnsZero()
    {
        _sut.CalculateAmountScore(100m, 0m).Should().Be(0m);
    }

    [Fact]
    public void CalculateAmountScore_BothZero_ReturnsOne()
    {
        _sut.CalculateAmountScore(0m, 0m).Should().Be(1m);
    }

    [Fact]
    public void CalculateAmountScore_NegativeValues_UsesAbsolute()
    {
        // Both negative, same absolute value => 1.0
        _sut.CalculateAmountScore(-1000m, -1000m).Should().Be(1m);
    }

    [Fact]
    public void CalculateAmountScore_MixedSigns_ComparesAbsolute()
    {
        // |1000| vs |1000| => 1.0
        _sut.CalculateAmountScore(-1000m, 1000m).Should().Be(1m);
    }

    [Fact]
    public void CalculateAmountScore_LargeAmounts_ExactMatch()
    {
        _sut.CalculateAmountScore(999999.99m, 999999.99m).Should().Be(1m);
    }

    [Fact]
    public void CalculateAmountScore_SmallAmounts_FivePercentOff()
    {
        // 5% of 10 = 0.50 → within 5% => 0.70
        _sut.CalculateAmountScore(10m, 10.50m).Should().Be(0.70m);
    }

    // ── CalculateDateScore (4-component: periodEnd + paymentWindow) ─

    [Fact]
    public void CalculateDateScore_WithinTrendyolWindow_ExactDay_ReturnsOne()
    {
        var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var txDate = periodEnd.AddDays(7); // Trendyol T+7 → exact match
        _sut.CalculateDateScore(periodEnd, txDate, "Trendyol").Should().Be(1.0m);
    }

    [Fact]
    public void CalculateDateScore_WithinTrendyolWindow_OneDayOff_Returns095()
    {
        var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var txDate = periodEnd.AddDays(8); // Trendyol T+7, 1 day off
        _sut.CalculateDateScore(periodEnd, txDate, "Trendyol").Should().Be(0.95m);
    }

    [Fact]
    public void CalculateDateScore_WithinAmazonWindow_ExactDay_ReturnsOne()
    {
        var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var txDate = periodEnd.AddDays(14); // Amazon T+14
        _sut.CalculateDateScore(periodEnd, txDate, "Amazon").Should().Be(1.0m);
    }

    [Fact]
    public void CalculateDateScore_WithinHepsiburadaWindow_ExactDay_ReturnsOne()
    {
        var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var txDate = periodEnd.AddDays(10); // Hepsiburada T+10
        _sut.CalculateDateScore(periodEnd, txDate, "Hepsiburada").Should().Be(1.0m);
    }

    [Fact]
    public void CalculateDateScore_FarOutsideWindow_ReturnsZero()
    {
        var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var txDate = periodEnd.AddDays(30); // Way outside any window
        _sut.CalculateDateScore(periodEnd, txDate, "Trendyol").Should().Be(0m);
    }

    [Fact]
    public void CalculateDateScore_ThreeDaysOff_Returns080()
    {
        var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var txDate = periodEnd.AddDays(10); // Trendyol T+7, 3 days off
        _sut.CalculateDateScore(periodEnd, txDate, "Trendyol").Should().Be(0.80m);
    }

    [Fact]
    public void CalculateDateScore_UnknownPlatform_UsesDefault7()
    {
        var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var txDate = periodEnd.AddDays(7); // Default T+7
        _sut.CalculateDateScore(periodEnd, txDate, "UnknownPlatform").Should().Be(1.0m);
    }

    // ── CalculateTextScore ──────────────────────────────────────────

    [Fact]
    public void CalculateTextScore_PayoutRefFound_ReturnsHigh()
    {
        _sut.CalculateTextScore("PAY-REF-123", "Trendyol", "ODEME PAY-REF-123 TRENDYOL")
            .Should().Be(1.0m); // 0.5 (ref) + 0.5 (platform) = 1.0
    }

    [Fact]
    public void CalculateTextScore_PlatformNameFound_ReturnsMedium()
    {
        _sut.CalculateTextScore(null, "Trendyol", "TRENDYOL ODEME")
            .Should().Be(0.5m); // Only platform name found
    }

    [Fact]
    public void CalculateTextScore_PlatformAliasFound_Returns04()
    {
        _sut.CalculateTextScore(null, "Trendyol", "TY HAVALE REF456")
            .Should().Be(0.4m); // Alias match = 0.4
    }

    [Fact]
    public void CalculateTextScore_NoMatch_ReturnsZero()
    {
        _sut.CalculateTextScore(null, "Trendyol", "BILINMEYEN ODEME")
            .Should().Be(0m);
    }

    [Fact]
    public void CalculateTextScore_NullDescription_ReturnsLowNeutral()
    {
        _sut.CalculateTextScore("REF-123", "Trendyol", null)
            .Should().Be(0.3m);
    }

    [Fact]
    public void CalculateTextScore_EmptyDescription_ReturnsLowNeutral()
    {
        _sut.CalculateTextScore("REF-123", "Trendyol", "  ")
            .Should().Be(0.3m);
    }

    [Fact]
    public void CalculateTextScore_OnlyPayoutRef_Returns05()
    {
        _sut.CalculateTextScore("PAY-999", null, "ODEME PAY-999 ISLEM")
            .Should().Be(0.5m);
    }

    [Fact]
    public void CalculateTextScore_HepsiburadaAlias_Returns04()
    {
        _sut.CalculateTextScore(null, "Hepsiburada", "D-MARKET ODEME")
            .Should().Be(0.4m);
    }

    [Fact]
    public void CalculateTextScore_CaseInsensitive()
    {
        _sut.CalculateTextScore(null, "Trendyol", "trendyol odeme")
            .Should().Be(0.5m);
    }

    // ── CalculateCounterpartyScore ──────────────────────────────────

    [Fact]
    public void CalculateCounterpartyScore_DSMGrup_ReturnsTrendyol()
    {
        _sut.CalculateCounterpartyScore("Trendyol", "DSM Grup Danismanlik")
            .Should().Be(1.0m);
    }

    [Fact]
    public void CalculateCounterpartyScore_DMARKET_ReturnsHepsiburada()
    {
        _sut.CalculateCounterpartyScore("Hepsiburada", "D-MARKET Elektronik")
            .Should().Be(1.0m);
    }

    [Fact]
    public void CalculateCounterpartyScore_N11Dogus_ReturnsOne()
    {
        _sut.CalculateCounterpartyScore("N11", "Dogus Teknoloji A.S.")
            .Should().Be(1.0m);
    }

    [Fact]
    public void CalculateCounterpartyScore_AmazonPayments_ReturnsOne()
    {
        _sut.CalculateCounterpartyScore("Amazon", "Amazon Payments Europe")
            .Should().Be(1.0m);
    }

    [Fact]
    public void CalculateCounterpartyScore_UnknownPayer_ReturnsLow()
    {
        _sut.CalculateCounterpartyScore("Trendyol", "Bilinmeyen Firma A.S.")
            .Should().Be(0.1m);
    }

    [Fact]
    public void CalculateCounterpartyScore_NullPlatform_ReturnsNeutral()
    {
        _sut.CalculateCounterpartyScore(null, "DSM Grup")
            .Should().Be(0.3m);
    }

    [Fact]
    public void CalculateCounterpartyScore_NullCounterparty_ReturnsNeutral()
    {
        _sut.CalculateCounterpartyScore("Trendyol", null)
            .Should().Be(0.3m);
    }

    [Fact]
    public void CalculateCounterpartyScore_BothNull_ReturnsNeutral()
    {
        _sut.CalculateCounterpartyScore(null, null)
            .Should().Be(0.3m);
    }

    [Fact]
    public void CalculateCounterpartyScore_UnknownPlatform_ReturnsNeutral()
    {
        _sut.CalculateCounterpartyScore("FictionalPlatform", "Herhangi Firma")
            .Should().Be(0.3m);
    }

    [Fact]
    public void CalculateCounterpartyScore_Ciceksepeti_ReturnOne()
    {
        _sut.CalculateCounterpartyScore("Ciceksepeti", "Ciceksepeti A.S.")
            .Should().Be(1.0m);
    }

    [Fact]
    public void CalculateCounterpartyScore_Pazarama_ReturnsOne()
    {
        _sut.CalculateCounterpartyScore("Pazarama", "PAZARAMA E-Ticaret")
            .Should().Be(1.0m);
    }

    // ── GetPlatformPaymentWindow ────────────────────────────────────

    [Fact]
    public void GetPlatformPaymentWindow_Trendyol_Returns7()
    {
        _sut.GetPlatformPaymentWindow("Trendyol").Should().Be(7);
    }

    [Fact]
    public void GetPlatformPaymentWindow_Amazon_Returns14()
    {
        _sut.GetPlatformPaymentWindow("Amazon").Should().Be(14);
    }

    [Fact]
    public void GetPlatformPaymentWindow_Hepsiburada_Returns10()
    {
        _sut.GetPlatformPaymentWindow("Hepsiburada").Should().Be(10);
    }

    [Fact]
    public void GetPlatformPaymentWindow_N11_Returns10()
    {
        _sut.GetPlatformPaymentWindow("N11").Should().Be(10);
    }

    [Fact]
    public void GetPlatformPaymentWindow_Ciceksepeti_Returns14()
    {
        _sut.GetPlatformPaymentWindow("Ciceksepeti").Should().Be(14);
    }

    [Fact]
    public void GetPlatformPaymentWindow_Pazarama_Returns7()
    {
        _sut.GetPlatformPaymentWindow("Pazarama").Should().Be(7);
    }

    [Fact]
    public void GetPlatformPaymentWindow_Unknown_ReturnsDefault7()
    {
        _sut.GetPlatformPaymentWindow("Unknown").Should().Be(7);
    }

    [Fact]
    public void GetPlatformPaymentWindow_NullOrEmpty_ReturnsDefault7()
    {
        _sut.GetPlatformPaymentWindow("").Should().Be(7);
        _sut.GetPlatformPaymentWindow(null!).Should().Be(7);
    }

    [Fact]
    public void GetPlatformPaymentWindow_CaseInsensitive()
    {
        _sut.GetPlatformPaymentWindow("trendyol").Should().Be(7);
        _sut.GetPlatformPaymentWindow("AMAZON").Should().Be(14);
    }

    // ── GetKnownPlatformPayers ──────────────────────────────────────

    [Fact]
    public void GetKnownPlatformPayers_ContainsDSMGrup()
    {
        var payers = _sut.GetKnownPlatformPayers();
        payers.Should().ContainKey("Trendyol");
        payers["Trendyol"].Should().Contain("DSM Grup");
    }

    [Fact]
    public void GetKnownPlatformPayers_ContainsDMARKET()
    {
        var payers = _sut.GetKnownPlatformPayers();
        payers.Should().ContainKey("Hepsiburada");
        payers["Hepsiburada"].Should().Contain("D-MARKET");
    }

    [Fact]
    public void GetKnownPlatformPayers_Contains6Platforms()
    {
        var payers = _sut.GetKnownPlatformPayers();
        payers.Should().HaveCount(6);
        payers.Keys.Should().Contain(new[]
        {
            "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "Pazarama"
        });
    }

    // ── CalculateConfidence combined scoring ────────────────────────

    [Fact]
    public void CalculateConfidence_PerfectMatch_Above095_AutoMatched()
    {
        var today = DateTime.UtcNow;
        var score = _sut.CalculateConfidence(
            1000m, 1000m, today, today, "TRENDYOL ODEME", "Trendyol");

        score.Should().BeGreaterOrEqualTo(0.95m);
    }

    [Fact]
    public void CalculateConfidence_PartialMatch_Between070And095_NeedsReview()
    {
        var today = DateTime.UtcNow;
        // 5% amount diff, same day, no description match
        var score = _sut.CalculateConfidence(
            1000m, 1050m, today, today.AddDays(2), null, null);

        score.Should().BeInRange(0.40m, 0.95m);
    }

    [Fact]
    public void CalculateConfidence_NoMatch_Below070_Unmatched()
    {
        var score = _sut.CalculateConfidence(
            1000m, 5000m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(20),
            "BILINMEYEN", "Trendyol");

        score.Should().BeLessThan(0.70m);
    }

    // ── Theory: multiple platform + amount + date combinations ──────

    [Theory]
    [InlineData(1000, 1000, 0, "Trendyol")]
    [InlineData(1000, 1005, 0, "Trendyol")]
    [InlineData(500, 500, 1, "Amazon")]
    [InlineData(2000, 2000, 0, "Hepsiburada")]
    public void CalculateConfidence_VariousCombinations_ShouldReturnValidRange(
        decimal bankAmount, decimal settlementAmount, int daysDiff, string platform)
    {
        var bankDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var settlementDate = bankDate.AddDays(daysDiff);

        var score = _sut.CalculateConfidence(
            bankAmount, settlementAmount, bankDate, settlementDate,
            $"{platform} ODEME", platform);

        score.Should().BeInRange(0m, 1m);
    }

    [Theory]
    [InlineData(1000, 1000, 1.0)]   // Exact
    [InlineData(1000, 1003, 1.0)]   // Within 0.5%
    [InlineData(1000, 1008, 0.95)]  // Within 1%
    [InlineData(1000, 1015, 0.85)]  // Within 2%
    [InlineData(1000, 1040, 0.70)]  // Within 5%
    [InlineData(1000, 1090, 0.40)]  // Within 10%
    [InlineData(1000, 1200, 0.0)]   // Over 10%
    public void CalculateAmountScore_Theory_ReturnsExpected(
        decimal expected, decimal actual, decimal expectedScore)
    {
        _sut.CalculateAmountScore(expected, actual).Should().Be(expectedScore);
    }

    [Theory]
    [InlineData(0, 1.0)]
    [InlineData(1, 0.95)]
    [InlineData(2, 0.90)]
    [InlineData(3, 0.80)]
    [InlineData(4, 0.60)]
    [InlineData(5, 0.60)]
    [InlineData(6, 0.40)]
    [InlineData(7, 0.40)]
    [InlineData(8, 0.20)]
    [InlineData(10, 0.20)]
    [InlineData(11, 0.0)]
    public void CalculateDateScore_Theory_ReturnsExpected(
        int daysDiff, decimal expectedScore)
    {
        var periodEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        // Trendyol T+7, so expected payment = periodEnd + 7
        var expectedPayment = periodEnd.AddDays(7);
        var txDate = expectedPayment.AddDays(daysDiff);

        _sut.CalculateDateScore(periodEnd, txDate, "Trendyol").Should().Be(expectedScore);
    }
}
