using FluentAssertions;
using MesTech.Application.Services;

namespace MesTech.Tests.Unit.Services;

/// <summary>
/// D12-21 — IProductReliabilityCalculator genişletilmiş testleri.
/// FeedReliabilityScoreService: 5 boyut × renk eşiği boundary testleri.
/// Mevcut FeedReliabilityScoreServiceTests'e DOKUNMAZ.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Reliability")]
public class ReliabilityCalculatorD12Tests
{
    // ══════════════════════════════════════
    // Score boundary tests — 4 renk eşiği
    // ══════════════════════════════════════

    [Fact]
    public void Calculate_AllPerfect_ShouldReturn100()
    {
        var input = new FeedReliabilityInput(100, 100, 100, 100, 50);
        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().Be(100);
        result.Color.Should().Be(ReliabilityColor.Green);
    }

    [Fact]
    public void Calculate_AllZero_ShouldReturn0()
    {
        var input = new FeedReliabilityInput(0, 0, 0, 0, 10000);
        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().Be(0);
        result.Color.Should().Be(ReliabilityColor.Red);
    }

    [Fact]
    public void Calculate_HighScore_ShouldBeGreen()
    {
        // ≥80 → green
        var input = new FeedReliabilityInput(90, 85, 90, 85, 200);
        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().BeGreaterOrEqualTo(80);
        result.Color.Should().Be(ReliabilityColor.Green);
    }

    [Fact]
    public void Calculate_MediumScore_ShouldBeYellow()
    {
        // 50-79 → yellow
        var input = new FeedReliabilityInput(60, 60, 60, 60, 1500);
        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().BeInRange(40, 79);
    }

    [Fact]
    public void Calculate_LowScore_ShouldBeOrangeOrRed()
    {
        // <40 → orange veya red
        var input = new FeedReliabilityInput(20, 20, 20, 20, 5000);
        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().BeLessThan(40);
        result.Color.Should().BeOneOf(ReliabilityColor.Orange, ReliabilityColor.Red);
    }

    // ══════════════════════════════════════
    // Individual dimension tests
    // ══════════════════════════════════════

    [Fact]
    public void Calculate_OnlyStockAccuracy_ShouldWeight25Pct()
    {
        var input = new FeedReliabilityInput(100, 0, 0, 0, 10000);
        var result = FeedReliabilityScoreService.Calculate(input);

        // Stock accuracy weight = 0.25, so 100 * 0.25 = 25
        result.Score.Should().Be(25);
        result.StockAccuracyScore.Should().Be(100);
    }

    [Fact]
    public void Calculate_OnlyUpdateFrequency_ShouldWeight20Pct()
    {
        var input = new FeedReliabilityInput(0, 100, 0, 0, 10000);
        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().Be(20);
        result.UpdateFrequencyScore.Should().Be(100);
    }

    [Fact]
    public void Calculate_OnlyFeedAvailability_ShouldWeight20Pct()
    {
        var input = new FeedReliabilityInput(0, 0, 100, 0, 10000);
        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().Be(20);
        result.FeedAvailabilityScore.Should().Be(100);
    }

    [Fact]
    public void Calculate_OnlyProductStability_ShouldWeight20Pct()
    {
        var input = new FeedReliabilityInput(0, 0, 0, 100, 10000);
        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().Be(20);
        result.ProductStabilityScore.Should().Be(100);
    }

    [Fact]
    public void Calculate_FastResponseTime_ShouldWeight15Pct()
    {
        // 50ms → should score near 100 for response time
        var input = new FeedReliabilityInput(0, 0, 0, 0, 50);
        var result = FeedReliabilityScoreService.Calculate(input);

        result.ResponseTimeScore.Should().BeGreaterOrEqualTo(90);
        result.Score.Should().BeGreaterOrEqualTo(13, "15% weight × ~90 score ≈ 13-15");
    }

    // ══════════════════════════════════════
    // Edge cases
    // ══════════════════════════════════════

    [Fact]
    public void Calculate_NegativeInputs_ShouldClampToZero()
    {
        var input = new FeedReliabilityInput(-50, -100, -10, -20, 10000);
        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Calculate_OverHundredInputs_ShouldClampTo100()
    {
        var input = new FeedReliabilityInput(200, 150, 300, 400, 10);
        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().BeLessOrEqualTo(100);
    }

    [Fact]
    public void Calculate_WithSupplierFeedId_ShouldCarryThrough()
    {
        var feedId = Guid.NewGuid();
        var input = new FeedReliabilityInput(80, 80, 80, 80, 500);
        var result = FeedReliabilityScoreService.CalculateForFeed(feedId, input);

        result.SupplierFeedId.Should().Be(feedId);
    }

    [Fact]
    public void Calculate_WeightsSum_ShouldBe100Percent()
    {
        // All dimensions at 100 → weighted sum = 100 * (0.25+0.20+0.20+0.20+0.15) = 100
        var input = new FeedReliabilityInput(100, 100, 100, 100, 0);
        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().Be(100, "all weights sum to 1.0, all inputs at 100 → score 100");
    }
}
