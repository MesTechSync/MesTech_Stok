using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Services;

namespace MesTech.Tests.Unit.Services;

[Trait("Category", "Unit")]
[Trait("Layer", "D12")]
public class ProductReliabilityCalculatorTests
{
    private readonly ProductReliabilityCalculator _sut = new();

    // ═══ Perfect Score ═══

    [Fact]
    public void Calculate_PerfectInput_ShouldReturnGreen100()
    {
        var input = new ProductReliabilityInput(
            SupplierReliabilityScore: 100,
            ReturnRate: 0, ComplaintRate: 0,
            AverageRating: 5.0m, TotalReviews: 100,
            SalesLast30Days: 50, StockConsistencyRate: 100,
            AverageDeliveryDays: 1, DamageRate: 0, OnTimeDeliveryRate: 100);

        var result = _sut.Calculate(input);

        result.OverallScore.Should().Be(100);
        result.OverallColor.Should().Be(ReliabilityColor.Green);
        result.SupplierScore.Should().Be(100);
        result.QualityScore.Should().Be(100);
        result.SalesScore.Should().Be(100);
        result.LogisticsScore.Should().Be(100);
    }

    // ═══ Worst Score ═══

    [Fact]
    public void Calculate_WorstInput_ShouldReturnRed()
    {
        var input = new ProductReliabilityInput(
            SupplierReliabilityScore: 0,
            ReturnRate: 10, ComplaintRate: 10,
            AverageRating: 1.0m, TotalReviews: 100,
            SalesLast30Days: 0, StockConsistencyRate: 0,
            AverageDeliveryDays: 10, DamageRate: 10, OnTimeDeliveryRate: 50);

        var result = _sut.Calculate(input);

        result.OverallColor.Should().Be(ReliabilityColor.Red);
        result.OverallScore.Should().BeLessThan(30);
    }

    // ═══ Color Thresholds ═══

    [Theory]
    [InlineData(95, ReliabilityColor.Green)]
    [InlineData(90, ReliabilityColor.Green)]
    [InlineData(89, ReliabilityColor.Yellow)]
    [InlineData(70, ReliabilityColor.Yellow)]
    [InlineData(69, ReliabilityColor.Orange)]
    [InlineData(50, ReliabilityColor.Orange)]
    [InlineData(49, ReliabilityColor.Red)]
    [InlineData(0, ReliabilityColor.Red)]
    public void Calculate_SupplierScoreThreshold_ShouldMapToCorrectColor(
        decimal supplierScore, ReliabilityColor expectedColor)
    {
        var input = new ProductReliabilityInput(
            SupplierReliabilityScore: supplierScore,
            ReturnRate: 0, ComplaintRate: 0,
            AverageRating: 5.0m, TotalReviews: 100,
            SalesLast30Days: 50, StockConsistencyRate: 100,
            AverageDeliveryDays: 1, DamageRate: 0, OnTimeDeliveryRate: 100);

        var result = _sut.Calculate(input);

        result.SupplierColor.Should().Be(expectedColor);
    }

    // ═══ Dimension Weights (30/25/25/20) ═══

    [Fact]
    public void Calculate_OnlySupplierHigh_ShouldContribute30Percent()
    {
        var input = new ProductReliabilityInput(
            SupplierReliabilityScore: 100,
            ReturnRate: 10, ComplaintRate: 10,
            AverageRating: 1.0m, TotalReviews: 100,
            SalesLast30Days: 0, StockConsistencyRate: 0,
            AverageDeliveryDays: 10, DamageRate: 10, OnTimeDeliveryRate: 50);

        var result = _sut.Calculate(input);

        // Supplier 100×0.3 = 30 + others near 0 → overall ~30
        result.OverallScore.Should().BeInRange(25, 40);
        result.SupplierScore.Should().Be(100);
    }

    // ═══ Quality Dimension ═══

    [Fact]
    public void Calculate_HighReturnRate_ShouldLowerQuality()
    {
        var input = new ProductReliabilityInput(
            SupplierReliabilityScore: 80,
            ReturnRate: 5, ComplaintRate: 0,  // 5% iade → returnScore = 0
            AverageRating: 4.5m, TotalReviews: 50,
            SalesLast30Days: 30, StockConsistencyRate: 90,
            AverageDeliveryDays: 2, DamageRate: 0, OnTimeDeliveryRate: 95);

        var result = _sut.Calculate(input);

        // returnScore=0, quality daha düşük
        result.QualityScore.Should().BeLessThan(80);
    }

    [Fact]
    public void Calculate_FewReviews_ShouldUseNeutralRating()
    {
        var input = new ProductReliabilityInput(
            SupplierReliabilityScore: 80,
            ReturnRate: 1, ComplaintRate: 0.5m,
            AverageRating: 1.0m, TotalReviews: 3,  // <5 reviews → neutral 50
            SalesLast30Days: 20, StockConsistencyRate: 85,
            AverageDeliveryDays: 2, DamageRate: 1, OnTimeDeliveryRate: 90);

        var result = _sut.Calculate(input);

        // 3 review → ratingScore=50 (neutral, not penalized for 1.0 avg)
        result.QualityScore.Should().BeGreaterThan(50);
    }

    // ═══ Sales Dimension ═══

    [Fact]
    public void Calculate_ZeroSales_ShouldGetBaseScore()
    {
        var input = new ProductReliabilityInput(
            SupplierReliabilityScore: 80,
            ReturnRate: 1, ComplaintRate: 0.5m,
            AverageRating: 4.0m, TotalReviews: 20,
            SalesLast30Days: 0,  // velocity = 20 (base)
            StockConsistencyRate: 100,
            AverageDeliveryDays: 2, DamageRate: 0, OnTimeDeliveryRate: 95);

        var result = _sut.Calculate(input);

        // velocity=20×0.6 + stock=100×0.4 = 12+40 = 52
        result.SalesScore.Should().BeInRange(50, 55);
    }

    // ═══ Logistics Dimension ═══

    [Fact]
    public void Calculate_FastDelivery_ShouldScoreHigh()
    {
        var input = new ProductReliabilityInput(
            SupplierReliabilityScore: 80,
            ReturnRate: 1, ComplaintRate: 0.5m,
            AverageRating: 4.0m, TotalReviews: 20,
            SalesLast30Days: 20, StockConsistencyRate: 90,
            AverageDeliveryDays: 1,  // max delivery score
            DamageRate: 0,           // max damage score
            OnTimeDeliveryRate: 100); // max ontime score

        var result = _sut.Calculate(input);

        result.LogisticsScore.Should().Be(100);
        result.LogisticsColor.Should().Be(ReliabilityColor.Green);
    }

    // ═══ Null Guard ═══

    [Fact]
    public void Calculate_NullInput_ShouldThrow()
    {
        var act = () => _sut.Calculate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ═══ Clamping ═══

    [Fact]
    public void Calculate_SupplierAbove100_ShouldClamp()
    {
        var input = new ProductReliabilityInput(
            SupplierReliabilityScore: 150,  // above max
            ReturnRate: 0, ComplaintRate: 0,
            AverageRating: 5.0m, TotalReviews: 100,
            SalesLast30Days: 50, StockConsistencyRate: 100,
            AverageDeliveryDays: 1, DamageRate: 0, OnTimeDeliveryRate: 100);

        var result = _sut.Calculate(input);

        result.SupplierScore.Should().Be(100);  // clamped
    }
}
