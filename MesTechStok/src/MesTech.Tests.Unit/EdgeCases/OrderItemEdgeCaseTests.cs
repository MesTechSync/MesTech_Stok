using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.EdgeCases;

/// <summary>
/// OrderItem edge case tests — amount calculation, boundary values.
/// Dalga 14 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class OrderItemEdgeCaseTests
{
    [Fact]
    public void CalculateAmounts_ShouldSetTotalPriceAndTax()
    {
        var item = new OrderItem
        {
            Quantity = 3,
            UnitPrice = 100m,
            TaxRate = 0.18m
        };

        item.CalculateAmounts();

        item.TotalPrice.Should().Be(300m);
        item.TaxAmount.Should().Be(54m);
    }

    [Fact]
    public void CalculateAmounts_ZeroQuantity_ShouldReturnZero()
    {
        var item = new OrderItem
        {
            Quantity = 0,
            UnitPrice = 100m,
            TaxRate = 0.18m
        };

        item.CalculateAmounts();

        item.TotalPrice.Should().Be(0m);
        item.TaxAmount.Should().Be(0m);
    }

    [Fact]
    public void CalculateAmounts_ZeroTaxRate_ShouldHaveNoTax()
    {
        var item = new OrderItem
        {
            Quantity = 5,
            UnitPrice = 50m,
            TaxRate = 0m
        };

        item.CalculateAmounts();

        item.TotalPrice.Should().Be(250m);
        item.TaxAmount.Should().Be(0m);
    }

    [Fact]
    public void SubTotal_ShouldBeQuantityTimesUnitPrice()
    {
        var item = new OrderItem
        {
            Quantity = 4,
            UnitPrice = 75m
        };

        item.SubTotal.Should().Be(300m);
    }

    [Fact]
    public void ToString_ShouldContainProductNameAndQuantity()
    {
        var item = new OrderItem
        {
            ProductName = "Test Product",
            Quantity = 2,
            TotalPrice = 200m
        };

        var str = item.ToString();

        str.Should().Contain("Test Product");
        str.Should().Contain("x2");
    }

    [Fact]
    public void DefaultProductName_ShouldBeEmpty()
    {
        var item = new OrderItem();
        item.ProductName.Should().BeEmpty();
        item.ProductSKU.Should().BeEmpty();
    }
}
