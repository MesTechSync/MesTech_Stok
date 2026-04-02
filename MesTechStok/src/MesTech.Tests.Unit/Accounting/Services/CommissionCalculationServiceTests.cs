using FluentAssertions;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Tests.Unit.Accounting.Services;

[Trait("Category", "Unit")]
public class CommissionCalculationServiceTests
{
    private readonly CommissionCalculationService _sut = new();

    [Theory]
    [InlineData("Trendyol", 1000, 150)]
    [InlineData("Hepsiburada", 1000, 180)]
    [InlineData("N11", 1000, 120)]
    [InlineData("Ciceksepeti", 1000, 200)]
    [InlineData("Amazon", 1000, 150)]
    [InlineData("Pazarama", 1000, 100)]
    public void CalculateCommission_ForKnownPlatform_ShouldReturnCorrect(
        string platform, decimal gross, decimal expected)
    {
        var result = _sut.CalculateCommission(platform, null, gross);

        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateCommission_ForUnknownPlatform_ShouldUseDefaultZeroPercent()
    {
        var result = _sut.CalculateCommission("UnknownPlatform", null, 1000m);

        result.Should().Be(0m); // Unknown platform → 0% (no commission assumed)
    }

    [Theory]
    [InlineData("Trendyol", 0.15)]
    [InlineData("Hepsiburada", 0.18)]
    [InlineData("N11", 0.12)]
    [InlineData("Ciceksepeti", 0.20)]
    [InlineData("Amazon", 0.15)]
    [InlineData("Pazarama", 0.10)]
    public void GetDefaultRate_ForKnownPlatform_ShouldReturnCorrect(
        string platform, decimal expectedRate)
    {
        var rate = _sut.GetDefaultRate(platform);

        rate.Should().Be(expectedRate);
    }

    [Fact]
    public void GetDefaultRate_ForUnknownPlatform_ShouldReturnZero()
    {
        var rate = _sut.GetDefaultRate("BilinmeyenPlatform");

        rate.Should().Be(0m); // Unknown platform → 0 (no commission assumed)
    }

    [Fact]
    public void CalculateCommission_CaseInsensitive_ShouldWork()
    {
        var lower = _sut.CalculateCommission("trendyol", null, 1000m);
        var upper = _sut.CalculateCommission("TRENDYOL", null, 1000m);

        lower.Should().Be(upper);
    }

    [Fact]
    public void CalculateCommission_WithZeroGrossAmount_ShouldReturnZero()
    {
        var result = _sut.CalculateCommission("Trendyol", null, 0m);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateCommission_WithNegativeGrossAmount_ShouldThrow()
    {
        var act = () => _sut.CalculateCommission("Trendyol", null, -1000m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateCommission_WithEmptyPlatform_ShouldThrow()
    {
        var act = () => _sut.CalculateCommission("", null, 1000m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateCommission_WithNullPlatform_ShouldThrow()
    {
        var act = () => _sut.CalculateCommission(null!, null, 1000m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateCommission_ShouldRoundToTwoDecimals()
    {
        // 333 * 0.15 = 49.95
        var result = _sut.CalculateCommission("Trendyol", null, 333m);

        result.Should().Be(49.95m);
    }

    [Fact]
    public void CalculateCommission_WithCategory_ShouldStillUseDefaultRate()
    {
        // Currently category does not affect rate; uses platform default
        var withCategory = _sut.CalculateCommission("Trendyol", "Elektronik", 1000m);
        var withoutCategory = _sut.CalculateCommission("Trendyol", null, 1000m);

        withCategory.Should().Be(withoutCategory);
    }

    [Fact]
    public void CalculateCommission_LargeAmount_ShouldHandleCorrectly()
    {
        var result = _sut.CalculateCommission("Trendyol", null, 1_000_000m);

        result.Should().Be(150_000m);
    }

    [Fact]
    public void CalculateCommission_SmallAmount_ShouldHandleCorrectly()
    {
        var result = _sut.CalculateCommission("Trendyol", null, 1m);

        result.Should().Be(0.15m);
    }

    [Theory]
    [InlineData("Trendyol", 2500, 375)]
    [InlineData("Hepsiburada", 3000, 540)]
    [InlineData("N11", 500, 60)]
    [InlineData("Amazon", 10000, 1500)]
    [InlineData("Ciceksepeti", 750, 150)]
    public void CalculateCommission_VariousAmounts_ShouldBeCorrect(
        string platform, decimal gross, decimal expected)
    {
        var result = _sut.CalculateCommission(platform, null, gross);

        result.Should().Be(expected);
    }

    [Fact]
    public void GetDefaultRate_CaseInsensitive_ShouldWork()
    {
        var lower = _sut.GetDefaultRate("hepsiburada");
        var upper = _sut.GetDefaultRate("HEPSIBURADA");

        lower.Should().Be(upper);
    }

    [Fact]
    public void CalculateCommission_ForPazarama_ShouldBe10Percent()
    {
        var result = _sut.CalculateCommission("Pazarama", null, 1000m);

        result.Should().Be(100m);
    }
}
