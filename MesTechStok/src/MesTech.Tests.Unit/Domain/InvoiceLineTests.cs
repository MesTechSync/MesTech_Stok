using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// InvoiceLine entity hesaplama testleri.
/// Bu testler kirilirsa = fatura satir hesaplama mantigi bozulmus demektir.
/// </summary>
[Trait("Category", "Unit")]
public class InvoiceLineTests
{
    [Fact]
    public void CalculateLineTotal_ShouldComputeCorrectly()
    {
        var line = new InvoiceLine
        {
            ProductName = "Test Urun",
            Quantity = 3,
            UnitPrice = 100m,
            TaxRate = 0.18m,
            DiscountAmount = 50m
        };

        line.CalculateLineTotal();

        // subtotal = 100 * 3 - 50 = 250
        // TaxAmount = 250 * 0.18 = 45
        // LineTotal = 250 + 45 = 295
        line.TaxAmount.Should().Be(45m);
        line.LineTotal.Should().Be(295m);
    }

    [Fact]
    public void CalculateLineTotal_WithZeroDiscount_ShouldComputeCorrectly()
    {
        var line = new InvoiceLine
        {
            ProductName = "Test Urun",
            Quantity = 2,
            UnitPrice = 150m,
            TaxRate = 0.18m,
            DiscountAmount = 0m
        };

        line.CalculateLineTotal();

        // subtotal = 150 * 2 - 0 = 300
        // TaxAmount = 300 * 0.18 = 54
        // LineTotal = 300 + 54 = 354
        line.TaxAmount.Should().Be(54m);
        line.LineTotal.Should().Be(354m);
    }

    [Fact]
    public void CalculateLineTotal_With20PercentTaxRate_ShouldComputeCorrectly()
    {
        var line = new InvoiceLine
        {
            ProductName = "KDV 20 Urun",
            Quantity = 1,
            UnitPrice = 500m,
            TaxRate = 0.20m,
            DiscountAmount = null
        };

        line.CalculateLineTotal();

        // subtotal = 500 * 1 - 0 = 500
        // TaxAmount = 500 * 0.20 = 100
        // LineTotal = 500 + 100 = 600
        line.TaxAmount.Should().Be(100m);
        line.LineTotal.Should().Be(600m);
    }
}
