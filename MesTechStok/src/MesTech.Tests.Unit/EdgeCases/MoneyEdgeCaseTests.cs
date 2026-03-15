using FluentAssertions;
using MesTech.Domain.ValueObjects;

namespace MesTech.Tests.Unit.EdgeCases;

/// <summary>
/// Money value object edge case tests — negative amounts, large values, boundary operations.
/// Dalga 14 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class MoneyEdgeCaseTests
{
    [Fact]
    public void Add_NegativeAmount_ShouldWork()
    {
        var a = Money.TRY(100m);
        var b = Money.TRY(-30m);

        var result = a.Add(b);

        result.Amount.Should().Be(70m);
    }

    [Fact]
    public void Subtract_ResultingInNegative_ShouldWork()
    {
        var a = Money.TRY(50m);
        var b = Money.TRY(100m);

        var result = a.Subtract(b);

        result.Amount.Should().Be(-50m);
    }

    [Fact]
    public void Multiply_ByZero_ShouldReturnZero()
    {
        var money = Money.TRY(500m);

        var result = money.Multiply(0);

        result.Amount.Should().Be(0m);
        result.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Multiply_ByNegative_ShouldReturnNegative()
    {
        var money = Money.TRY(100m);

        var result = money.Multiply(-2);

        result.Amount.Should().Be(-200m);
    }

    [Fact]
    public void Multiply_ByFraction_ShouldWork()
    {
        var money = Money.TRY(100m);

        var result = money.Multiply(0.18m);

        result.Amount.Should().Be(18m);
    }

    [Fact]
    public void Zero_WithUSD_ShouldReturnZeroUSD()
    {
        var money = Money.Zero("USD");

        money.Amount.Should().Be(0m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Factory_EUR_ShouldSetCurrency()
    {
        var money = Money.EUR(250m);

        money.Amount.Should().Be(250m);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Equality_DifferentCurrency_ShouldNotBeEqual()
    {
        var a = new Money(100m, "TRY");
        var b = new Money(100m, "USD");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equality_DifferentAmount_ShouldNotBeEqual()
    {
        var a = Money.TRY(100m);
        var b = Money.TRY(200m);

        a.Should().NotBe(b);
    }

    [Fact]
    public void ToString_ShouldContainAmountAndCurrency()
    {
        var money = new Money(1_234.56m, "EUR");

        var str = money.ToString();

        str.Should().Contain("EUR");
        (str.Contains("1,234.56") || str.Contains("1.234,56")).Should().BeTrue("amount should be formatted with either dot or comma separator");
    }

    [Fact]
    public void Add_ZeroToZero_ShouldReturnZero()
    {
        var a = Money.Zero();
        var b = Money.Zero();

        var result = a.Add(b);

        result.Amount.Should().Be(0m);
    }

    [Fact]
    public void Subtract_DifferentCurrency_ShouldThrowWithMessage()
    {
        var a = Money.TRY(100m);
        var b = Money.USD(50m);

        var act = () => a.Subtract(b);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TRY*USD*");
    }

    [Fact]
    public void Add_DifferentCurrency_ShouldThrowWithMessage()
    {
        var a = Money.EUR(100m);
        var b = Money.TRY(50m);

        var act = () => a.Add(b);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EUR*TRY*");
    }
}
