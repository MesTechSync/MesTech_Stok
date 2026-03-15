using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Tests.Unit.EdgeCases;

/// <summary>
/// Invoice entity edge case tests — state transitions, cancellation rules, boundary values.
/// Dalga 14 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class InvoiceEdgeCaseTests
{
    [Fact]
    public void Cancel_WithReason_ShouldSetReasonAndTimestamp()
    {
        var invoice = new Invoice { Status = InvoiceStatus.Draft };

        invoice.Cancel("Musteri istegi ile iptal");

        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
        invoice.CancellationReason.Should().Be("Musteri istegi ile iptal");
        invoice.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Cancel_WithoutReason_ShouldSetNullReason()
    {
        var invoice = new Invoice { Status = InvoiceStatus.Draft };

        invoice.Cancel();

        invoice.CancellationReason.Should().BeNull();
        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ShouldRaiseInvoiceCancelledEvent()
    {
        var orderId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Status = InvoiceStatus.Draft,
            OrderId = orderId,
            InvoiceNumber = "INV-CANCEL-01"
        };

        invoice.Cancel("Test iptal");

        invoice.DomainEvents.Should().ContainSingle();
        var evt = invoice.DomainEvents[0] as InvoiceCancelledEvent;
        evt.Should().NotBeNull();
        evt!.OrderId.Should().Be(orderId);
        evt.InvoiceNumber.Should().Be("INV-CANCEL-01");
        evt.Reason.Should().Be("Test iptal");
    }

    [Fact]
    public void Cancel_FromSent_ShouldSucceed()
    {
        var invoice = new Invoice { Status = InvoiceStatus.Sent };

        var act = () => invoice.Cancel("Gonderim hatasi");

        act.Should().NotThrow();
        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromRejected_ShouldSucceed()
    {
        var invoice = new Invoice { Status = InvoiceStatus.Rejected };

        var act = () => invoice.Cancel();

        act.Should().NotThrow();
        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void AddLine_ZeroQuantity_ShouldAddLineWithZeroTotal()
    {
        var invoice = new Invoice();
        invoice.AddLine(new InvoiceLine
        {
            ProductName = "Zero Qty",
            Quantity = 0,
            UnitPrice = 100m,
            TaxAmount = 0m,
            DiscountAmount = 0m
        });

        invoice.Lines.Should().HaveCount(1);
        invoice.SubTotal.Should().Be(0m);
    }

    [Fact]
    public void AddLine_WithDiscount_ShouldReduceSubTotal()
    {
        var invoice = new Invoice();
        invoice.AddLine(new InvoiceLine
        {
            ProductName = "Discounted",
            Quantity = 1,
            UnitPrice = 500m,
            TaxAmount = 81m,
            DiscountAmount = 50m
        });

        // SubTotal = 500*1 - 50 = 450
        invoice.SubTotal.Should().Be(450m);
        invoice.TaxTotal.Should().Be(81m);
        invoice.GrandTotal.Should().Be(531m);
    }

    [Fact]
    public void MarkAsSent_WithNullGibId_ShouldStillSetStatus()
    {
        var invoice = new Invoice();

        invoice.MarkAsSent(null, null);

        invoice.Status.Should().Be(InvoiceStatus.Sent);
        invoice.GibInvoiceId.Should().BeNull();
        invoice.PdfUrl.Should().BeNull();
        invoice.SentAt.Should().NotBeNull();
    }

    [Fact]
    public void CreateForOrder_WithNullCustomerName_ShouldDefaultToEmpty()
    {
        var order = new Order
        {
            TenantId = Guid.NewGuid(),
            CustomerName = null,
            SubTotal = 100m,
            TaxAmount = 18m,
            TotalAmount = 118m
        };

        var invoice = Invoice.CreateForOrder(order, InvoiceType.EArsiv, "INV-NULL-01");

        invoice.CustomerName.Should().BeEmpty();
    }

    [Fact]
    public void ToString_ShouldContainInvoiceNumberAndStatus()
    {
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-TS-01",
            Status = InvoiceStatus.Sent,
            GrandTotal = 250m
        };

        var str = invoice.ToString();

        str.Should().Contain("INV-TS-01");
        str.Should().Contain("Sent");
    }

    [Fact]
    public void CreateForOrder_WithEIrsaliye_ShouldNotSetEInvoiceTaxpayer()
    {
        var order = new Order { TenantId = Guid.NewGuid(), CustomerName = "Test" };
        var invoice = Invoice.CreateForOrder(order, InvoiceType.EIrsaliye, "INV-IRS-01");

        invoice.IsEInvoiceTaxpayer.Should().BeFalse();
        invoice.Type.Should().Be(InvoiceType.EIrsaliye);
    }

    [Fact]
    public void Lines_ShouldBeReadOnlyCollection()
    {
        var invoice = new Invoice();
        invoice.Lines.Should().BeAssignableTo<IReadOnlyCollection<InvoiceLine>>();
    }
}
