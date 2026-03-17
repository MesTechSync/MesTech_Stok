using FluentAssertions;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Tests.Unit.Accounting.Services;

[Trait("Category", "Unit")]
public class KdvCalculationServiceTests
{
    private readonly KdvCalculationService _sut = new();

    [Fact]
    public void Calculate_StandardScenario_ShouldReturnCorrectValues()
    {
        // 1000 TL sale, 20% KDV, 12.99% commission, 0% withholding
        var input = new KdvCalculationInput(1000m, 0.20m, 0.1299m, 0m);

        var result = _sut.Calculate(input);

        result.GrossSale.Should().Be(1000m);
        result.KdvAmount.Should().Be(200m);
        result.TotalWithKdv.Should().Be(1200m);
        result.Commission.Should().Be(129.90m);
        result.CommissionKdv.Should().Be(25.98m);
        result.Withholding.Should().Be(0m);
        result.NetToSeller.Should().Be(1044.12m);
    }

    [Fact]
    public void Calculate_WithWithholding_ShouldDeductFromNet()
    {
        // 1000 TL sale, 20% KDV, 12.99% commission, 20% withholding
        var input = new KdvCalculationInput(1000m, 0.20m, 0.1299m, 0.20m);

        var result = _sut.Calculate(input);

        result.Withholding.Should().Be(200m);
        // Net = 1200 - 129.90 - 25.98 - 200 = 844.12
        result.NetToSeller.Should().Be(844.12m);
    }

    [Fact]
    public void Calculate_ZeroSale_ShouldReturnAllZeros()
    {
        var input = new KdvCalculationInput(0m, 0.20m, 0.10m, 0m);

        var result = _sut.Calculate(input);

        result.GrossSale.Should().Be(0m);
        result.KdvAmount.Should().Be(0m);
        result.Commission.Should().Be(0m);
        result.NetToSeller.Should().Be(0m);
    }

    [Fact]
    public void Calculate_KdvRate1Percent_ShouldComputeCorrectly()
    {
        // %1 KDV (temel gida)
        var input = new KdvCalculationInput(1000m, 0.01m, 0.10m, 0m);

        var result = _sut.Calculate(input);

        result.KdvAmount.Should().Be(10m);
        result.TotalWithKdv.Should().Be(1010m);
    }

    [Fact]
    public void Calculate_KdvRate10Percent_ShouldComputeCorrectly()
    {
        // %10 KDV
        var input = new KdvCalculationInput(1000m, 0.10m, 0.10m, 0m);

        var result = _sut.Calculate(input);

        result.KdvAmount.Should().Be(100m);
        result.TotalWithKdv.Should().Be(1100m);
    }

    [Fact]
    public void Calculate_NegativeGrossSale_ShouldThrow()
    {
        var input = new KdvCalculationInput(-100m, 0.20m, 0.10m, 0m);

        var act = () => _sut.Calculate(input);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calculate_KdvRateAboveOne_ShouldThrow()
    {
        var input = new KdvCalculationInput(1000m, 1.5m, 0.10m, 0m);

        var act = () => _sut.Calculate(input);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calculate_NegativeKdvRate_ShouldThrow()
    {
        var input = new KdvCalculationInput(1000m, -0.1m, 0.10m, 0m);

        var act = () => _sut.Calculate(input);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calculate_CommissionRateAboveOne_ShouldThrow()
    {
        var input = new KdvCalculationInput(1000m, 0.20m, 1.5m, 0m);

        var act = () => _sut.Calculate(input);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calculate_NegativeCommissionRate_ShouldThrow()
    {
        var input = new KdvCalculationInput(1000m, 0.20m, -0.1m, 0m);

        var act = () => _sut.Calculate(input);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calculate_WithholdingRateAboveOne_ShouldThrow()
    {
        var input = new KdvCalculationInput(1000m, 0.20m, 0.10m, 1.5m);

        var act = () => _sut.Calculate(input);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calculate_NegativeWithholdingRate_ShouldThrow()
    {
        var input = new KdvCalculationInput(1000m, 0.20m, 0.10m, -0.1m);

        var act = () => _sut.Calculate(input);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calculate_ShouldReturnBreakdownText()
    {
        var input = new KdvCalculationInput(1000m, 0.20m, 0.10m, 0m);

        var result = _sut.Calculate(input);

        result.Breakdown.Should().NotBeNullOrEmpty();
        result.Breakdown.Should().Contain("KDV Hesaplama Detayi");
        result.Breakdown.Should().Contain("Brut Satis");
    }

    [Fact]
    public void Calculate_WithWithholding_BreakdownShouldContainStopaj()
    {
        var input = new KdvCalculationInput(1000m, 0.20m, 0.10m, 0.20m);

        var result = _sut.Calculate(input);

        result.Breakdown.Should().Contain("Stopaj");
    }

    [Fact]
    public void Calculate_WithoutWithholding_BreakdownShouldSayNotApplied()
    {
        var input = new KdvCalculationInput(1000m, 0.20m, 0.10m, 0m);

        var result = _sut.Calculate(input);

        result.Breakdown.Should().Contain("Uygulanmiyor");
    }

    [Fact]
    public void Calculate_VerifyNetFormula()
    {
        // Net = TotalWithKdv - Commission - CommissionKdv - Withholding
        var input = new KdvCalculationInput(5000m, 0.20m, 0.15m, 0.10m);

        var result = _sut.Calculate(input);

        var expectedNet = result.TotalWithKdv - result.Commission - result.CommissionKdv - result.Withholding;
        result.NetToSeller.Should().Be(expectedNet);
    }
}
