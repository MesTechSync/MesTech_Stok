using FluentAssertions;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Tests.Unit.Accounting.Services;

[Trait("Category", "Unit")]
public class ProfitCalculationServiceTests
{
    private readonly ProfitCalculationService _sut = new();

    [Theory]
    [InlineData(10000, 6000, 1500, 500, 200, 1800)]
    [InlineData(5000, 3000, 750, 250, 100, 900)]
    [InlineData(1000, 500, 100, 50, 20, 330)]
    [InlineData(0, 0, 0, 0, 0, 0)]
    public void CalculateNetProfit_ShouldReturnCorrectValue(
        decimal revenue, decimal cost, decimal commission,
        decimal cargo, decimal tax, decimal expected)
    {
        var result = _sut.CalculateNetProfit(revenue, cost, commission, cargo, tax);

        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateNetProfit_WithAllZeros_ShouldReturnZero()
    {
        var result = _sut.CalculateNetProfit(0m, 0m, 0m, 0m, 0m);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateNetProfit_CanReturnNegative()
    {
        // When costs exceed revenue
        var result = _sut.CalculateNetProfit(1000m, 800m, 150m, 100m, 50m);

        result.Should().Be(-100m);
    }

    [Fact]
    public void CalculateNetProfit_ShouldRoundToTwoDecimals()
    {
        var result = _sut.CalculateNetProfit(1000.555m, 500.333m, 100.111m, 50.055m, 20.027m);

        // 1000.555 - 500.333 - 100.111 - 50.055 - 20.027 = 330.029 -> 330.03
        result.Should().Be(330.03m);
    }

    [Fact]
    public void CalculateNetProfit_WithOnlyRevenue_ShouldReturnRevenue()
    {
        var result = _sut.CalculateNetProfit(5000m, 0m, 0m, 0m, 0m);

        result.Should().Be(5000m);
    }

    [Fact]
    public void CalculateNetProfit_WithOnlyCost_ShouldReturnNegative()
    {
        var result = _sut.CalculateNetProfit(0m, 3000m, 0m, 0m, 0m);

        result.Should().Be(-3000m);
    }

    [Fact]
    public void CalculateNetProfit_WithLargeAmounts_ShouldHandleCorrectly()
    {
        var result = _sut.CalculateNetProfit(
            1_000_000m, 600_000m, 150_000m, 50_000m, 20_000m);

        result.Should().Be(180_000m);
    }

    [Theory]
    [InlineData(10000, 1800, 18.00)]
    [InlineData(5000, 1000, 20.00)]
    [InlineData(1000, -100, -10.00)]
    [InlineData(1000, 0, 0)]
    public void CalculateProfitMargin_ShouldReturnPercentage(
        decimal revenue, decimal netProfit, decimal expectedMargin)
    {
        var result = _sut.CalculateProfitMargin(revenue, netProfit);

        result.Should().Be(expectedMargin);
    }

    [Fact]
    public void CalculateProfitMargin_WithZeroRevenue_ShouldReturnZero()
    {
        var result = _sut.CalculateProfitMargin(0m, 1000m);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateProfitMargin_ShouldRoundToTwoDecimals()
    {
        // 333 / 1000 * 100 = 33.30
        var result = _sut.CalculateProfitMargin(1000m, 333m);

        result.Should().Be(33.30m);
    }

    [Fact]
    public void CalculateProfitMargin_NegativeProfit_ShouldReturnNegative()
    {
        var result = _sut.CalculateProfitMargin(1000m, -200m);

        result.Should().Be(-20.00m);
    }

    [Fact]
    public void CalculateProfitMargin_100PercentMargin_ShouldReturn100()
    {
        var result = _sut.CalculateProfitMargin(1000m, 1000m);

        result.Should().Be(100.00m);
    }

    [Fact]
    public void CalculateProfitMargin_MoreThan100Percent_ShouldWork()
    {
        // When netProfit > revenue (unlikely but mathematically possible)
        var result = _sut.CalculateProfitMargin(500m, 1000m);

        result.Should().Be(200.00m);
    }
}
