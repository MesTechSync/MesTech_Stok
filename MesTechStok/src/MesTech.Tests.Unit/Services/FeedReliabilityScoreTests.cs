using FluentAssertions;
using MesTech.Application.Services;

namespace MesTech.Tests.Unit.Services;

/// <summary>
/// DEV 5 — FeedReliabilityScoreService tests (static pure calc).
/// Score formula: StockAccuracy(25%) + UpdateFrequency(20%) + FeedAvailability(20%)
///                + ProductStability(20%) + ResponseTime(15%) = 0-100
/// Colors: Green(90-100), Yellow(75-89), Orange(50-74), Red(0-49)
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "ReliabilityScore")]
public class FeedReliabilityScoreTests
{
    [Fact]
    public void PerfectSupplier_Returns_Green()
    {
        var input = new FeedReliabilityInput(
            StockAccuracyPercent: 100,
            UpdateFrequencyPercent: 100,
            FeedAvailabilityPercent: 100,
            ProductStabilityPercent: 100,
            AverageResponseTimeMs: 200);

        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().BeGreaterOrEqualTo(90);
        result.Color.Should().Be(ReliabilityColor.Green);
    }

    [Fact]
    public void GoodSupplier_Returns_Yellow()
    {
        // Weighted: 85*0.25 + 80*0.20 + 90*0.20 + 75*0.20 + responseTime(1200ms)*0.15
        // responseTime(1200ms) → 100 - (1200-500)/(2000-500)*50 ≈ 76.67
        // Weighted ≈ 21.25 + 16 + 18 + 15 + 11.5 ≈ 81.75 → 82
        var input = new FeedReliabilityInput(
            StockAccuracyPercent: 85,
            UpdateFrequencyPercent: 80,
            FeedAvailabilityPercent: 90,
            ProductStabilityPercent: 75,
            AverageResponseTimeMs: 1200);

        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().BeInRange(75, 89);
        result.Color.Should().Be(ReliabilityColor.Yellow);
    }

    [Fact]
    public void MediumSupplier_Returns_Orange()
    {
        // responseTime(3500ms) → 50 - (3500-2000)/(5000-2000)*30 = 50 - 15 = 35
        // Weighted: 60*0.25 + 50*0.20 + 70*0.20 + 55*0.20 + 35*0.15 = 15+10+14+11+5.25 = 55.25
        var input = new FeedReliabilityInput(
            StockAccuracyPercent: 60,
            UpdateFrequencyPercent: 50,
            FeedAvailabilityPercent: 70,
            ProductStabilityPercent: 55,
            AverageResponseTimeMs: 3500);

        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().BeInRange(50, 74);
        result.Color.Should().Be(ReliabilityColor.Orange);
    }

    [Fact]
    public void PoorSupplier_Returns_Red()
    {
        // responseTime(6000ms) → 0 (> 5000ms)
        // Weighted: 20*0.25 + 10*0.20 + 30*0.20 + 25*0.20 + 0*0.15 = 5+2+6+5+0 = 18
        var input = new FeedReliabilityInput(
            StockAccuracyPercent: 20,
            UpdateFrequencyPercent: 10,
            FeedAvailabilityPercent: 30,
            ProductStabilityPercent: 25,
            AverageResponseTimeMs: 6000);

        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().BeLessThan(50);
        result.Color.Should().Be(ReliabilityColor.Red);
    }

    [Fact]
    public void ScoreCalculation_IsDeterministic()
    {
        var input = new FeedReliabilityInput(
            StockAccuracyPercent: 72,
            UpdateFrequencyPercent: 65,
            FeedAvailabilityPercent: 88,
            ProductStabilityPercent: 50,
            AverageResponseTimeMs: 1000);

        var result1 = FeedReliabilityScoreService.Calculate(input);
        var result2 = FeedReliabilityScoreService.Calculate(input);

        result1.Score.Should().Be(result2.Score);
        result1.Color.Should().Be(result2.Color);
    }

    [Fact]
    public void CalculateForFeed_AssociatesFeedId()
    {
        var feedId = Guid.NewGuid();
        var input = new FeedReliabilityInput(100, 100, 100, 100, 100);

        var result = FeedReliabilityScoreService.CalculateForFeed(feedId, input);

        result.SupplierFeedId.Should().Be(feedId);
    }

    [Theory]
    [InlineData(95, ReliabilityColor.Green)]
    [InlineData(90, ReliabilityColor.Green)]
    [InlineData(89, ReliabilityColor.Yellow)]
    [InlineData(75, ReliabilityColor.Yellow)]
    [InlineData(74, ReliabilityColor.Orange)]
    [InlineData(50, ReliabilityColor.Orange)]
    [InlineData(49, ReliabilityColor.Red)]
    [InlineData(0, ReliabilityColor.Red)]
    public void ColorMapping_ShouldMatchScoreRange(int score, ReliabilityColor expectedColor)
    {
        var color = score switch
        {
            >= 90 => ReliabilityColor.Green,
            >= 75 => ReliabilityColor.Yellow,
            >= 50 => ReliabilityColor.Orange,
            _ => ReliabilityColor.Red
        };
        color.Should().Be(expectedColor);
    }

    [Fact]
    public void ResponseTimeAbove5000ms_ShouldGiveZeroScore()
    {
        var input = new FeedReliabilityInput(100, 100, 100, 100, 10000);

        var result = FeedReliabilityScoreService.Calculate(input);

        result.ResponseTimeScore.Should().Be(0);
        result.Score.Should().BeLessThan(100);
    }

    [Fact]
    public void AllZeroMetrics_ShouldReturnRed()
    {
        var input = new FeedReliabilityInput(0, 0, 0, 0, 10000);

        var result = FeedReliabilityScoreService.Calculate(input);

        result.Score.Should().Be(0);
        result.Color.Should().Be(ReliabilityColor.Red);
    }
}
