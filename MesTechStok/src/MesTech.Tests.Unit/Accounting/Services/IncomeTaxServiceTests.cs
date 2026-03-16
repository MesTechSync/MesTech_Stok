using FluentAssertions;
using MesTech.Infrastructure.Finance;

namespace MesTech.Tests.Unit.Accounting.Services;

[Trait("Category", "Unit")]
public class IncomeTaxServiceTests
{
    private readonly IncomeTaxService _sut = new();

    [Fact]
    public void CalculateIncomeTax_ZeroIncome_ShouldReturnZeroTax()
    {
        var result = _sut.CalculateIncomeTax(0m, 2026);

        result.TotalTax.Should().Be(0m);
        result.EffectiveRate.Should().Be(0m);
    }

    [Fact]
    public void CalculateIncomeTax_FirstBracketOnly_ShouldBe15Percent()
    {
        // 100.000 TL — fully in first bracket (0-110K @ 15%)
        var result = _sut.CalculateIncomeTax(100_000m, 2026);

        result.TotalTax.Should().Be(15_000m); // 100K * 0.15
        result.EffectiveRate.Should().Be(0.15m);
    }

    [Fact]
    public void CalculateIncomeTax_ExactFirstBracketBoundary_ShouldBe15Percent()
    {
        var result = _sut.CalculateIncomeTax(110_000m, 2026);

        result.TotalTax.Should().Be(16_500m); // 110K * 0.15
        result.EffectiveRate.Should().Be(0.15m);
    }

    [Fact]
    public void CalculateIncomeTax_SecondBracket_ShouldCalculateCorrectly()
    {
        // 200.000 TL — 110K @ 15% = 16.500 + 90K @ 20% = 18.000 → 34.500
        var result = _sut.CalculateIncomeTax(200_000m, 2026);

        result.TotalTax.Should().Be(34_500m);
        result.BracketDetails.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public void CalculateIncomeTax_ThirdBracket_ShouldCalculateCorrectly()
    {
        // 300.000 TL
        // 110K @ 15% = 16.500
        // 120K @ 20% = 24.000
        //  70K @ 27% = 18.900
        // Total = 59.400
        var result = _sut.CalculateIncomeTax(300_000m, 2026);

        result.TotalTax.Should().Be(59_400m);
    }

    [Fact]
    public void CalculateIncomeTax_HighIncome_ShouldUseAllBrackets()
    {
        // 5.000.000 TL
        // 110K @ 15% = 16.500
        // 120K @ 20% = 24.000
        // 350K @ 27% = 94.500
        // 2.420K @ 35% = 847.000
        // 2.000K @ 40% = 800.000
        // Total = 1.782.000
        var result = _sut.CalculateIncomeTax(5_000_000m, 2026);

        result.TotalTax.Should().Be(1_782_000m);
        result.BracketDetails.Should().HaveCount(5);
    }

    [Fact]
    public void CalculateIncomeTax_EffectiveRate_ShouldBeCorrect()
    {
        var result = _sut.CalculateIncomeTax(200_000m, 2026);

        // 34.500 / 200.000 = 0.1725
        result.EffectiveRate.Should().Be(0.1725m);
    }

    [Fact]
    public void CalculateIncomeTax_BracketDetails_ShouldSumToTotal()
    {
        var result = _sut.CalculateIncomeTax(500_000m, 2026);

        result.BracketDetails.Sum(b => b.TaxInBracket).Should().Be(result.TotalTax);
    }

    [Fact]
    public void CalculateIncomeTax_BracketDetails_TaxableAmountsShouldSumToIncome()
    {
        var income = 500_000m;
        var result = _sut.CalculateIncomeTax(income, 2026);

        result.BracketDetails.Sum(b => b.TaxableAmountInBracket).Should().Be(income);
    }

    [Fact]
    public void CalculateIncomeTax_NegativeIncome_ShouldThrow()
    {
        var act = () => _sut.CalculateIncomeTax(-1000m, 2026);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateIncomeTax_InvalidYear_ShouldThrow()
    {
        var act = () => _sut.CalculateIncomeTax(100_000m, 1999);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateIncomeTax_Year_ShouldBeSetInResult()
    {
        var result = _sut.CalculateIncomeTax(100_000m, 2026);

        result.Year.Should().Be(2026);
        result.TaxableIncome.Should().Be(100_000m);
    }
}
