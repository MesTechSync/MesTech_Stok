using FluentAssertions;
using MesTech.Domain.Services;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// Fiyatlama domain servisi koruma testleri.
/// </summary>
public class PricingServiceTests
{
    private readonly PricingService _sut = new();

    [Fact]
    public void CalculateProfitMargin_ShouldReturnCorrectPercentage()
    {
        // (100 - 60) / 100 * 100 = 40%
        _sut.CalculateProfitMargin(60, 100).Should().Be(40m);
    }

    [Fact]
    public void CalculateProfitMargin_ZeroSalePrice_ShouldReturnZero()
    {
        _sut.CalculateProfitMargin(50, 0).Should().Be(0m);
    }

    [Fact]
    public void ApplyDiscount_ShouldReducePrice()
    {
        // 100 TL, %20 indirim = 80 TL
        _sut.ApplyDiscount(100m, 20m).Should().Be(80m);
    }

    [Fact]
    public void ApplyDiscount_ZeroPercent_ShouldReturnSamePrice()
    {
        _sut.ApplyDiscount(100m, 0m).Should().Be(100m);
    }

    [Fact]
    public void ApplyDiscount_InvalidRate_ShouldReturnOriginalPrice()
    {
        _sut.ApplyDiscount(100m, -5m).Should().Be(100m);
        _sut.ApplyDiscount(100m, 150m).Should().Be(100m);
    }

    [Fact]
    public void CalculatePriceWithTax_ShouldAddKDV()
    {
        // 100 TL + %20 KDV = 120 TL
        _sut.CalculatePriceWithTax(100m, 0.20m).Should().Be(120m);
    }

    [Fact]
    public void CalculatePriceWithoutTax_ShouldRemoveKDV()
    {
        // 120 TL / 1.20 = 100 TL
        _sut.CalculatePriceWithoutTax(120m, 0.20m).Should().Be(100m);
    }
}
