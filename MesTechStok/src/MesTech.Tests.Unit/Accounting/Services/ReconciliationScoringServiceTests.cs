using FluentAssertions;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Tests.Unit.Accounting.Services;

[Trait("Category", "Unit")]
public class ReconciliationScoringServiceTests
{
    private readonly ReconciliationScoringService _sut = new();

    [Fact]
    public void AutoMatchThreshold_ShouldBe085()
    {
        _sut.AutoMatchThreshold.Should().Be(0.85m);
    }

    [Fact]
    public void CalculateConfidence_ExactAmountAndSameDate_ShouldReturnHighScore()
    {
        var today = DateTime.UtcNow;

        var score = _sut.CalculateConfidence(
            1000m, 1000m, today, today,
            "TRENDYOL ODEME", "Trendyol");

        score.Should().BeGreaterOrEqualTo(0.85m);
    }

    [Fact]
    public void CalculateConfidence_ExactMatchAllFactors_ShouldReturnMaxScore()
    {
        var today = DateTime.UtcNow;

        var score = _sut.CalculateConfidence(
            1000m, 1000m, today, today,
            "TRENDYOL ODEME", "Trendyol");

        // 1.0 * 0.60 + 1.0 * 0.25 + 1.0 * 0.15 = 1.0
        score.Should().Be(1.0m);
    }

    [Fact]
    public void CalculateConfidence_SameDateDifferentAmount_ShouldScoreLower()
    {
        var today = DateTime.UtcNow;

        var score = _sut.CalculateConfidence(
            1000m, 2000m, today, today);

        score.Should().BeLessThan(0.85m);
    }

    [Fact]
    public void CalculateConfidence_DateProximity_0Days_ShouldScoreHigh()
    {
        var today = DateTime.UtcNow;

        var score = _sut.CalculateConfidence(
            1000m, 1000m, today, today);

        // Date score = 1.0
        score.Should().BeGreaterOrEqualTo(0.85m);
    }

    [Fact]
    public void CalculateConfidence_DateProximity_1Day_ShouldScoreSlightlyLower()
    {
        var bankDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var settlementDate = bankDate.AddDays(1);

        var scoreExact = _sut.CalculateConfidence(1000m, 1000m, bankDate, bankDate);
        var score1Day = _sut.CalculateConfidence(1000m, 1000m, bankDate, settlementDate);

        score1Day.Should().BeLessThanOrEqualTo(scoreExact);
    }

    [Fact]
    public void CalculateConfidence_DateProximity_MoreThan7Days_ShouldScoreLow()
    {
        var bankDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var settlementDate = bankDate.AddDays(10);

        var score = _sut.CalculateConfidence(
            1000m, 1000m, bankDate, settlementDate);

        // Date score = 0 for > 7 days apart
        score.Should().BeLessThan(0.85m);
    }

    [Fact]
    public void CalculateConfidence_DescriptionContainsPlatformName_ShouldScoreHigher()
    {
        var today = DateTime.UtcNow;

        var withMatch = _sut.CalculateConfidence(
            1000m, 1000m, today, today,
            "TRENDYOL HAVALE", "Trendyol");

        var withoutMatch = _sut.CalculateConfidence(
            1000m, 1000m, today, today,
            "BILINMEYEN ODEME", "Trendyol");

        withMatch.Should().BeGreaterThan(withoutMatch);
    }

    [Fact]
    public void CalculateConfidence_DescriptionContainsAlias_ShouldScoreWell()
    {
        var today = DateTime.UtcNow;

        var score = _sut.CalculateConfidence(
            1000m, 1000m, today, today,
            "TY ODEME REF123", "Trendyol");

        score.Should().BeGreaterOrEqualTo(0.85m);
    }

    [Fact]
    public void CalculateConfidence_NullDescriptions_ShouldReturnNeutralDescriptionScore()
    {
        var today = DateTime.UtcNow;

        var score = _sut.CalculateConfidence(
            1000m, 1000m, today, today);

        // Neutral description score (0.5 * 0.15 = 0.075 contribution)
        score.Should().BeGreaterOrEqualTo(0.85m);
    }

    [Fact]
    public void CalculateConfidence_ShouldBeBetween0And1()
    {
        var score = _sut.CalculateConfidence(
            1000m, 5000m,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30));

        score.Should().BeInRange(0m, 1m);
    }

    [Fact]
    public void CalculateConfidence_BothZeroAmounts_ShouldScoreHighOnAmount()
    {
        var today = DateTime.UtcNow;

        var score = _sut.CalculateConfidence(0m, 0m, today, today);

        score.Should().BeGreaterOrEqualTo(0.85m);
    }

    [Fact]
    public void CalculateConfidence_OneZeroAmount_ShouldScoreLowOnAmount()
    {
        var today = DateTime.UtcNow;

        var score = _sut.CalculateConfidence(1000m, 0m, today, today);

        score.Should().BeLessThan(0.60m);
    }

    [Fact]
    public void CalculateConfidence_SmallAmountDifference_ShouldScoreHigh()
    {
        var today = DateTime.UtcNow;

        // Less than 1% difference
        var score = _sut.CalculateConfidence(
            1000m, 1005m, today, today,
            "TRENDYOL ODEME", "Trendyol");

        score.Should().BeGreaterOrEqualTo(0.85m);
    }
}
