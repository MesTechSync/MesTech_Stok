using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.EdgeCases;

/// <summary>
/// InvoiceLine edge case tests — line total calculation, discount, boundary values.
/// Dalga 14 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class InvoiceLineEdgeCaseTests
{
    [Fact]
    public void CalculateLineTotal_BasicCalculation_ShouldBeCorrect()
    {
        var line = new InvoiceLine
        {
            Quantity = 2,
            UnitPrice = 100m,
            TaxRate = 0.18m,
            DiscountAmount = 0m
        };

        line.CalculateLineTotal();

        // subtotal = 100*2 - 0 = 200; tax = 200*0.18 = 36; total = 236
        line.TaxAmount.Should().Be(36m);
        line.LineTotal.Should().Be(236m);
    }

    [Fact]
    public void CalculateLineTotal_WithDiscount_ShouldReduceSubtotal()
    {
        var line = new InvoiceLine
        {
            Quantity = 1,
            UnitPrice = 500m,
            TaxRate = 0.20m,
            DiscountAmount = 100m
        };

        line.CalculateLineTotal();

        // subtotal = 500 - 100 = 400; tax = 400*0.20 = 80; total = 480
        line.TaxAmount.Should().Be(80m);
        line.LineTotal.Should().Be(480m);
    }

    [Fact]
    public void CalculateLineTotal_NullDiscount_ShouldTreatAsZero()
    {
        var line = new InvoiceLine
        {
            Quantity = 3,
            UnitPrice = 50m,
            TaxRate = 0.08m,
            DiscountAmount = null
        };

        line.CalculateLineTotal();

        // subtotal = 50*3 - 0 = 150; tax = 150*0.08 = 12; total = 162
        line.TaxAmount.Should().Be(12m);
        line.LineTotal.Should().Be(162m);
    }

    [Fact]
    public void CalculateLineTotal_ZeroQuantity_ShouldBeZero()
    {
        var line = new InvoiceLine
        {
            Quantity = 0,
            UnitPrice = 100m,
            TaxRate = 0.18m,
            DiscountAmount = 0m
        };

        line.CalculateLineTotal();

        line.TaxAmount.Should().Be(0m);
        line.LineTotal.Should().Be(0m);
    }

    [Fact]
    public void CalculateLineTotal_ZeroTaxRate_ShouldHaveNoTax()
    {
        var line = new InvoiceLine
        {
            Quantity = 5,
            UnitPrice = 200m,
            TaxRate = 0m,
            DiscountAmount = 0m
        };

        line.CalculateLineTotal();

        line.TaxAmount.Should().Be(0m);
        line.LineTotal.Should().Be(1000m);
    }

    [Fact]
    public void CalculateLineTotal_DiscountExceedsSubtotal_ShouldThrow()
    {
        var line = new InvoiceLine
        {
            Quantity = 1,
            UnitPrice = 100m,
            TaxRate = 0.18m,
            DiscountAmount = 200m
        };

        var act = () => line.CalculateLineTotal();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*İndirim tutarı*brüt tutarı*aşamaz*");
    }

    [Fact]
    public void DefaultProductName_ShouldBeEmpty()
    {
        var line = new InvoiceLine();
        line.ProductName.Should().BeEmpty();
    }
}
