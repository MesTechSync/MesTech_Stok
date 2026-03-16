using FluentAssertions;
using MesTech.Infrastructure.Finance;

namespace MesTech.Tests.Unit.Accounting.Services;

[Trait("Category", "Unit")]
public class DepreciationServiceTests
{
    private readonly DepreciationService _sut = new();

    // ── Linear Method ──

    [Fact]
    public void CalculateDepreciation_Linear_ShouldReturnCorrectYears()
    {
        var result = _sut.CalculateDepreciation(10_000m, 5, "linear");

        result.Should().HaveCount(5);
    }

    [Fact]
    public void CalculateDepreciation_Linear_EachYearEqualAmount()
    {
        var result = _sut.CalculateDepreciation(10_000m, 5, "linear");

        result.Should().AllSatisfy(r => r.DepreciationAmount.Should().Be(2_000m));
    }

    [Fact]
    public void CalculateDepreciation_Linear_FinalBookValueZero()
    {
        var result = _sut.CalculateDepreciation(10_000m, 5, "linear");

        result.Last().BookValue.Should().Be(0m);
        result.Last().AccumulatedDepreciation.Should().Be(10_000m);
    }

    [Fact]
    public void CalculateDepreciation_Linear_AccumulationIncreases()
    {
        var result = _sut.CalculateDepreciation(12_000m, 4, "linear");

        for (int i = 1; i < result.Count; i++)
        {
            result[i].AccumulatedDepreciation.Should().BeGreaterThan(result[i - 1].AccumulatedDepreciation);
        }
    }

    [Fact]
    public void CalculateDepreciation_Linear_RoundingHandled()
    {
        // 10_000 / 3 = 3333.33... per year
        var result = _sut.CalculateDepreciation(10_000m, 3, "linear");

        result.Last().BookValue.Should().Be(0m);
        result.Sum(r => r.DepreciationAmount).Should().Be(10_000m);
    }

    // ── Declining Method ──

    [Fact]
    public void CalculateDepreciation_Declining_ShouldReturnCorrectYears()
    {
        var result = _sut.CalculateDepreciation(10_000m, 5, "declining");

        result.Should().HaveCount(5);
    }

    [Fact]
    public void CalculateDepreciation_Declining_FirstYearHigherThanLast()
    {
        var result = _sut.CalculateDepreciation(10_000m, 5, "declining");

        result.First().DepreciationAmount.Should().BeGreaterThan(result[1].DepreciationAmount);
    }

    [Fact]
    public void CalculateDepreciation_Declining_FinalBookValueZero()
    {
        var result = _sut.CalculateDepreciation(10_000m, 5, "declining");

        result.Last().BookValue.Should().Be(0m);
    }

    [Fact]
    public void CalculateDepreciation_Declining_TotalEqualsOriginalCost()
    {
        var result = _sut.CalculateDepreciation(10_000m, 5, "declining");

        result.Sum(r => r.DepreciationAmount).Should().Be(10_000m);
    }

    // ── Validation ──

    [Fact]
    public void CalculateDepreciation_ZeroCost_ShouldThrow()
    {
        var act = () => _sut.CalculateDepreciation(0m, 5, "linear");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateDepreciation_NegativeCost_ShouldThrow()
    {
        var act = () => _sut.CalculateDepreciation(-1000m, 5, "linear");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateDepreciation_ZeroYears_ShouldThrow()
    {
        var act = () => _sut.CalculateDepreciation(10_000m, 0, "linear");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateDepreciation_InvalidMethod_ShouldThrow()
    {
        var act = () => _sut.CalculateDepreciation(10_000m, 5, "invalid");

        act.Should().Throw<ArgumentException>().WithMessage("*Desteklenmeyen*");
    }

    [Fact]
    public void CalculateDepreciation_TurkishMethodNames_ShouldWork()
    {
        var dogrusal = _sut.CalculateDepreciation(10_000m, 5, "dogrusal");
        var azalan = _sut.CalculateDepreciation(10_000m, 5, "azalan");

        dogrusal.Should().HaveCount(5);
        azalan.Should().HaveCount(5);
    }
}
