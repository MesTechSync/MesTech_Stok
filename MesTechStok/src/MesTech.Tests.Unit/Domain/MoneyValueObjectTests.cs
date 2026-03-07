using FluentAssertions;
using MesTech.Domain.ValueObjects;

namespace MesTech.Tests.Unit.Domain;

public class MoneyValueObjectTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var money = new Money(100.50m, "TRY");

        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Create_DefaultCurrency_ShouldBeTRY()
    {
        var money = new Money(50m);
        money.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Add_SameCurrency_ShouldReturnSum()
    {
        var a = new Money(100m, "TRY");
        var b = new Money(50m, "TRY");

        var result = a.Add(b);

        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Add_DifferentCurrency_ShouldThrow()
    {
        var a = new Money(100m, "TRY");
        var b = new Money(50m, "USD");

        var act = () => a.Add(b);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtract_SameCurrency_ShouldReturnDifference()
    {
        var a = new Money(100m, "TRY");
        var b = new Money(30m, "TRY");

        var result = a.Subtract(b);

        result.Amount.Should().Be(70m);
    }

    [Fact]
    public void Subtract_DifferentCurrency_ShouldThrow()
    {
        var a = Money.TRY(100m);
        var b = Money.EUR(30m);

        var act = () => a.Subtract(b);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Multiply_ShouldReturnProduct()
    {
        var money = new Money(50m, "TRY");

        var result = money.Multiply(3);

        result.Amount.Should().Be(150m);
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var a = new Money(100m, "TRY");
        var b = new Money(100m, "TRY");

        a.Should().Be(b);
    }

    [Fact]
    public void Factory_TRY_ShouldSetCurrency()
    {
        var money = Money.TRY(200m);
        money.Amount.Should().Be(200m);
        money.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Factory_USD_ShouldSetCurrency()
    {
        var money = Money.USD(100m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Zero_ShouldReturnZeroAmount()
    {
        var money = Money.Zero();
        money.Amount.Should().Be(0);
        money.Currency.Should().Be("TRY");
    }
}
