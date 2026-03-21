using FluentAssertions;
using MesTech.Domain.ValueObjects;

namespace MesTech.Integration.Tests.Domain;

public class MoneyTests
{
    // --- Factory Methods ---

    [Fact]
    public void TRY_ShouldCreateMoneyWithTRYCurrency()
    {
        var money = Money.TRY(100);

        money.Amount.Should().Be(100);
        money.Currency.Should().Be("TRY");
    }

    [Fact]
    public void USD_ShouldCreateMoneyWithUSDCurrency()
    {
        var money = Money.USD(50);

        money.Amount.Should().Be(50);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void EUR_ShouldCreateMoneyWithEURCurrency()
    {
        var money = Money.EUR(75);

        money.Amount.Should().Be(75);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Zero_WithDefaultCurrency_ShouldReturnZeroTRY()
    {
        var money = Money.Zero();

        money.Amount.Should().Be(0);
        money.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Zero_WithExplicitCurrency_ShouldReturnZeroInThatCurrency()
    {
        var money = Money.Zero("USD");

        money.Amount.Should().Be(0);
        money.Currency.Should().Be("USD");
    }

    // --- Add ---

    [Fact]
    public void Add_SameCurrency_ShouldReturnSum()
    {
        var a = Money.TRY(100);
        var b = Money.TRY(50);

        var result = a.Add(b);

        result.Amount.Should().Be(150);
        result.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Add_DifferentCurrency_ShouldThrowInvalidOperationException()
    {
        var tryMoney = Money.TRY(100);
        var usdMoney = Money.USD(50);

        var act = () => tryMoney.Add(usdMoney);

        act.Should().Throw<InvalidOperationException>();
    }

    // --- Subtract ---

    [Fact]
    public void Subtract_SameCurrency_ShouldReturnDifference()
    {
        var a = Money.TRY(100);
        var b = Money.TRY(30);

        var result = a.Subtract(b);

        result.Amount.Should().Be(70);
        result.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Subtract_DifferentCurrency_ShouldThrowInvalidOperationException()
    {
        var tryMoney = Money.TRY(100);
        var eurMoney = Money.EUR(30);

        var act = () => tryMoney.Subtract(eurMoney);

        act.Should().Throw<InvalidOperationException>();
    }

    // --- Multiply ---

    [Fact]
    public void Multiply_ShouldReturnScaledAmount()
    {
        var money = Money.TRY(100);

        var result = money.Multiply(1.20m);

        result.Amount.Should().Be(120.0m);
        result.Currency.Should().Be("TRY");
    }

    // --- ToString ---

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var money = Money.TRY(100);

        money.ToString().Should().Be("100.00 TRY");
    }

    // --- Equality ---

    [Fact]
    public void Equality_SameAmountAndCurrency_ShouldBeEqual()
    {
        var a = Money.TRY(100);
        var b = Money.TRY(100);

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentAmount_ShouldNotBeEqual()
    {
        var a = Money.TRY(100);
        var b = Money.TRY(200);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equality_DifferentCurrency_ShouldNotBeEqual()
    {
        var a = Money.TRY(100);
        var b = Money.USD(100);

        a.Should().NotBe(b);
    }
}

public class StockLevelTests
{
    // --- IsLow ---

    [Theory]
    [InlineData(5, 5, true)]
    [InlineData(3, 5, true)]
    [InlineData(6, 5, false)]
    public void IsLow_ShouldReturnExpected(int current, int minimum, bool expected)
    {
        var level = new StockLevel(current, minimum, Maximum: 100, ReorderLevel: 20, ReorderQuantity: 50);

        level.IsLow.Should().Be(expected);
    }

    // --- IsCritical ---

    [Theory]
    [InlineData(5, 10, true)]   // 5 <= 10/2=5 → true
    [InlineData(4, 10, true)]   // 4 <= 5 → true
    [InlineData(6, 10, false)]  // 6 > 5 → false
    public void IsCritical_WithPositiveReorderLevel_ShouldReturnExpected(int current, int reorderLevel, bool expected)
    {
        var level = new StockLevel(current, Minimum: 5, Maximum: 100, ReorderLevel: reorderLevel, ReorderQuantity: 50);

        level.IsCritical.Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void IsCritical_WhenReorderLevelIsZero_ShouldReturnFalse(int current)
    {
        var level = new StockLevel(current, Minimum: 5, Maximum: 100, ReorderLevel: 0, ReorderQuantity: 50);

        level.IsCritical.Should().BeFalse();
    }

    // --- NeedsReorder ---

    [Theory]
    [InlineData(20, 20, true)]
    [InlineData(15, 20, true)]
    [InlineData(21, 20, false)]
    public void NeedsReorder_ShouldReturnExpected(int current, int reorderLevel, bool expected)
    {
        var level = new StockLevel(current, Minimum: 5, Maximum: 100, ReorderLevel: reorderLevel, ReorderQuantity: 50);

        level.NeedsReorder.Should().Be(expected);
    }

    // --- ReorderAmount ---

    [Fact]
    public void ReorderAmount_WhenNeedsReorder_ShouldReturnReorderQuantity()
    {
        var level = new StockLevel(Current: 10, Minimum: 5, Maximum: 100, ReorderLevel: 20, ReorderQuantity: 50);

        level.ReorderAmount.Should().Be(50);
    }

    [Fact]
    public void ReorderAmount_WhenDoesNotNeedReorder_ShouldReturnZero()
    {
        var level = new StockLevel(Current: 50, Minimum: 5, Maximum: 100, ReorderLevel: 20, ReorderQuantity: 50);

        level.ReorderAmount.Should().Be(0);
    }

    // --- IsOverStock ---

    [Theory]
    [InlineData(101, 100, true)]
    [InlineData(200, 100, true)]
    [InlineData(100, 100, false)]
    [InlineData(50, 100, false)]
    public void IsOverStock_WithPositiveMaximum_ShouldReturnExpected(int current, int maximum, bool expected)
    {
        var level = new StockLevel(current, Minimum: 5, Maximum: maximum, ReorderLevel: 20, ReorderQuantity: 50);

        level.IsOverStock.Should().Be(expected);
    }

    [Fact]
    public void IsOverStock_WhenMaximumIsZero_ShouldReturnFalse()
    {
        var level = new StockLevel(Current: 999, Minimum: 5, Maximum: 0, ReorderLevel: 20, ReorderQuantity: 50);

        level.IsOverStock.Should().BeFalse();
    }

    // --- IsOutOfStock ---

    [Theory]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    [InlineData(-10, true)]
    [InlineData(1, false)]
    [InlineData(50, false)]
    public void IsOutOfStock_ShouldReturnExpected(int current, bool expected)
    {
        var level = new StockLevel(current, Minimum: 5, Maximum: 100, ReorderLevel: 20, ReorderQuantity: 50);

        level.IsOutOfStock.Should().Be(expected);
    }

    // --- Status ---

    [Fact]
    public void Status_WhenOutOfStock_ShouldReturnOutOfStock()
    {
        var level = new StockLevel(Current: 0, Minimum: 5, Maximum: 100, ReorderLevel: 10, ReorderQuantity: 50);

        level.Status.Should().Be("OutOfStock");
    }

    [Fact]
    public void Status_WhenCritical_ShouldReturnCritical()
    {
        // Current=2, ReorderLevel=10 → 2 <= 10/2=5 → Critical
        var level = new StockLevel(Current: 2, Minimum: 5, Maximum: 100, ReorderLevel: 10, ReorderQuantity: 50);

        level.Status.Should().Be("Critical");
    }

    [Fact]
    public void Status_WhenLow_ShouldReturnLow()
    {
        // Current=5, Minimum=5 → IsLow=true, IsCritical needs check
        // ReorderLevel=0 → IsCritical=false, IsLow=true
        var level = new StockLevel(Current: 5, Minimum: 5, Maximum: 100, ReorderLevel: 0, ReorderQuantity: 50);

        level.Status.Should().Be("Low");
    }

    [Fact]
    public void Status_WhenOverStock_ShouldReturnOverStock()
    {
        // Current=200, Maximum=100 → IsOverStock=true
        // Must NOT be OutOfStock/Critical/Low to reach OverStock branch
        var level = new StockLevel(Current: 200, Minimum: 5, Maximum: 100, ReorderLevel: 10, ReorderQuantity: 50);

        level.Status.Should().Be("OverStock");
    }

    [Fact]
    public void Status_WhenNormal_ShouldReturnNormal()
    {
        var level = new StockLevel(Current: 50, Minimum: 10, Maximum: 100, ReorderLevel: 20, ReorderQuantity: 50);

        level.Status.Should().Be("Normal");
    }
}
