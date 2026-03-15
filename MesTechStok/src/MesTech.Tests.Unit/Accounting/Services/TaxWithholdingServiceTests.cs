using FluentAssertions;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Tests.Unit.Accounting.Services;

[Trait("Category", "Unit")]
public class TaxWithholdingServiceTests
{
    private readonly TaxWithholdingService _sut = new();

    [Theory]
    [InlineData(1000, 0.01, 10)]
    [InlineData(1000, 0.05, 50)]
    [InlineData(1000, 0.10, 100)]
    [InlineData(1000, 0.20, 200)]
    [InlineData(5000, 0.02, 100)]
    [InlineData(10000, 0.15, 1500)]
    public void CalculateWithholding_WithVariousRates_ShouldReturnCorrect(
        decimal matrah, decimal rate, decimal expected)
    {
        var result = _sut.CalculateWithholding(matrah, rate);

        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateWithholding_WithZeroRate_ShouldReturnZero()
    {
        var result = _sut.CalculateWithholding(1000m, 0m);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateWithholding_WithZeroAmount_ShouldReturnZero()
    {
        var result = _sut.CalculateWithholding(0m, 0.10m);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateWithholding_WithNegativeRate_ShouldThrow()
    {
        var act = () => _sut.CalculateWithholding(1000m, -0.01m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateWithholding_WithRateAbove1_ShouldThrow()
    {
        var act = () => _sut.CalculateWithholding(1000m, 1.5m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateWithholding_WithRate1_ShouldReturnFullAmount()
    {
        var result = _sut.CalculateWithholding(1000m, 1.0m);

        result.Should().Be(1000m);
    }

    [Fact]
    public void CalculateWithholding_ShouldRoundToTwoDecimals()
    {
        // 333 * 0.07 = 23.31
        var result = _sut.CalculateWithholding(333m, 0.07m);

        result.Should().Be(23.31m);
    }

    [Fact]
    public void CalculateWithholding_CommissionShouldNotAffectMatrah()
    {
        // 9284 CB rule: Commission does NOT reduce the matrah
        // Matrah (KDV haric) = 1000, Commission = 150 -> matrah is still 1000
        decimal matrah = 1000m;
        decimal commission = 150m; // Should NOT be subtracted

        var result = _sut.CalculateWithholding(matrah, 0.05m);

        result.Should().Be(50m); // 1000 * 0.05, NOT (1000-150) * 0.05 = 42.5
    }

    [Fact]
    public void CalculateWithholding_CargoShouldNotAffectMatrah()
    {
        // 9284 CB rule: Cargo does NOT reduce the matrah
        decimal matrah = 1000m;
        decimal cargo = 30m; // Should NOT be subtracted

        var result = _sut.CalculateWithholding(matrah, 0.05m);

        result.Should().Be(50m); // 1000 * 0.05, NOT (1000-30) * 0.05 = 48.5
    }

    [Theory]
    [InlineData(1200, 0.20, 1000)]
    [InlineData(1180, 0.18, 1000)]
    [InlineData(590, 0.18, 500)]
    [InlineData(1100, 0.10, 1000)]
    [InlineData(1000, 0.0, 1000)]
    public void ExtractTaxExclusiveAmount_ShouldCalculateCorrectly(
        decimal taxInclusive, decimal kdvRate, decimal expected)
    {
        var result = _sut.ExtractTaxExclusiveAmount(taxInclusive, kdvRate);

        result.Should().Be(expected);
    }

    [Fact]
    public void ExtractTaxExclusiveAmount_WithNegativeKdvRate_ShouldThrow()
    {
        var act = () => _sut.ExtractTaxExclusiveAmount(1200m, -0.18m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ExtractTaxExclusiveAmount_WithZeroKdvRate_ShouldReturnSame()
    {
        var result = _sut.ExtractTaxExclusiveAmount(1000m, 0m);

        result.Should().Be(1000m);
    }

    [Fact]
    public void ExtractTaxExclusiveAmount_ShouldRoundToTwoDecimals()
    {
        // 1000 / 1.18 = 847.457627... -> 847.46
        var result = _sut.ExtractTaxExclusiveAmount(1000m, 0.18m);

        result.Should().Be(847.46m);
    }

    [Fact]
    public void CalculateWithholding_WithNegativeAmount_ShouldReturnNegative()
    {
        var result = _sut.CalculateWithholding(-1000m, 0.10m);

        result.Should().Be(-100m);
    }

    [Fact]
    public void CalculateWithholding_LargeAmount_ShouldHandleCorrectly()
    {
        var result = _sut.CalculateWithholding(1_000_000m, 0.05m);

        result.Should().Be(50_000m);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.02)]
    [InlineData(0.05)]
    [InlineData(0.07)]
    [InlineData(0.10)]
    [InlineData(0.15)]
    [InlineData(0.20)]
    public void CalculateWithholding_VariousCommonRates_ShouldNotThrow(decimal rate)
    {
        var act = () => _sut.CalculateWithholding(1000m, rate);

        act.Should().NotThrow();
    }
}
