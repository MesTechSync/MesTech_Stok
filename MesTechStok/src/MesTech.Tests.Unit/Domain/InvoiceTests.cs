using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// Invoice entity domain logic koruma testleri.
/// Bu testler kirilirsa = fatura mantigi bozulmus demektir.
/// </summary>
[Trait("Category", "Unit")]
public class InvoiceTests
{
    [Fact]
    public void Invoice_DefaultStatus_ShouldBeDraft()
    {
        var invoice = new Invoice();

        invoice.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public void Invoice_DefaultDirection_ShouldBeOutgoing()
    {
        var invoice = new Invoice();

        invoice.Direction.Should().Be(InvoiceDirection.Outgoing);
    }

    [Fact]
    public void Invoice_DefaultCurrency_ShouldBeTRY()
    {
        var invoice = new Invoice();

        invoice.Currency.Should().Be("TRY");
    }

    [Fact]
    public void AddLine_ShouldAddLineAndRecalculateTotals()
    {
        var invoice = new Invoice();
        var line = new InvoiceLine
        {
            ProductName = "Test Urun",
            Quantity = 2,
            UnitPrice = 100m,
            TaxRate = 0.20m,
            TaxAmount = 40m,
            DiscountAmount = 0m
        };

        invoice.AddLine(line);

        invoice.Lines.Should().HaveCount(1);
        invoice.SubTotal.Should().Be(200m);
        invoice.TaxTotal.Should().Be(40m);
        invoice.GrandTotal.Should().Be(240m);
    }

    [Fact]
    public void AddLine_WithMultipleLines_ShouldCalculateCorrectTotals()
    {
        var invoice = new Invoice();
        var line1 = new InvoiceLine
        {
            ProductName = "Urun A",
            Quantity = 3,
            UnitPrice = 50m,
            TaxRate = 0.20m,
            TaxAmount = 30m,
            DiscountAmount = 10m
        };
        var line2 = new InvoiceLine
        {
            ProductName = "Urun B",
            Quantity = 1,
            UnitPrice = 200m,
            TaxRate = 0.18m,
            TaxAmount = 36m,
            DiscountAmount = 0m
        };

        invoice.AddLine(line1);
        invoice.AddLine(line2);

        // SubTotal = (50*3 - 10) + (200*1 - 0) = 140 + 200 = 340
        invoice.SubTotal.Should().Be(340m);
        // TaxTotal = 30 + 36 = 66
        invoice.TaxTotal.Should().Be(66m);
        // GrandTotal = 340 + 66 = 406
        invoice.GrandTotal.Should().Be(406m);
    }

    [Fact]
    public void MarkAsSent_ShouldSetStatusAndFieldsAndRaiseEvent()
    {
        var invoice = new Invoice { InvoiceNumber = "INV-001" };
        var gibId = "GIB-12345";
        var pdfUrl = "https://example.com/invoice.pdf";

        invoice.MarkAsSent(gibId, pdfUrl);

        invoice.Status.Should().Be(InvoiceStatus.Sent);
        invoice.GibInvoiceId.Should().Be(gibId);
        invoice.PdfUrl.Should().Be(pdfUrl);
        invoice.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        invoice.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<InvoiceSentEvent>();
    }

    [Fact]
    public void MarkAsAccepted_ShouldSetStatusAndAcceptedAt()
    {
        var invoice = new Invoice();

        invoice.MarkAsAccepted();

        invoice.Status.Should().Be(InvoiceStatus.Accepted);
        invoice.AcceptedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsRejected_ShouldSetStatusToRejected()
    {
        var invoice = new Invoice();

        invoice.MarkAsRejected();

        invoice.Status.Should().Be(InvoiceStatus.Rejected);
    }

    [Fact]
    public void Cancel_FromDraft_ShouldSetStatusToCancelled()
    {
        var invoice = new Invoice { Status = InvoiceStatus.Draft };

        invoice.Cancel();

        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromAccepted_ShouldThrowInvalidOperationException()
    {
        var invoice = new Invoice { Status = InvoiceStatus.Accepted };

        var act = () => invoice.Cancel();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*iptal*");
    }

    [Fact]
    public void MarkAsPlatformSent_ShouldSetUrlAndStatus()
    {
        var invoice = new Invoice();
        var url = "https://platform.example.com/invoice/123";

        invoice.MarkAsPlatformSent(url);

        invoice.PlatformInvoiceUrl.Should().Be(url);
        invoice.Status.Should().Be(InvoiceStatus.PlatformSent);
    }

    [Fact]
    public void CreateForOrder_ShouldCopyFieldsAndRaiseEvent()
    {
        var order = new Order
        {
            TenantId = Guid.NewGuid(),
            OrderNumber = "ORD-100",
            CustomerName = "Ahmet Yilmaz",
            CustomerEmail = "ahmet@example.com",
            SubTotal = 1000m,
            TaxAmount = 180m,
            TotalAmount = 1180m
        };

        var invoice = Invoice.CreateForOrder(order, InvoiceType.EFatura, "INV-100");

        invoice.OrderId.Should().Be(order.Id);
        invoice.TenantId.Should().Be(order.TenantId);
        invoice.Type.Should().Be(InvoiceType.EFatura);
        invoice.InvoiceNumber.Should().Be("INV-100");
        invoice.CustomerName.Should().Be("Ahmet Yilmaz");
        invoice.CustomerEmail.Should().Be("ahmet@example.com");
        invoice.SubTotal.Should().Be(1000m);
        invoice.TaxTotal.Should().Be(180m);
        invoice.GrandTotal.Should().Be(1180m);
        invoice.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<InvoiceCreatedEvent>();
    }

    [Fact]
    public void CreateForOrder_WithEFatura_ShouldSetIsEInvoiceTaxpayerTrue()
    {
        var order = new Order
        {
            TenantId = Guid.NewGuid(),
            CustomerName = "Test Musteri"
        };

        var invoice = Invoice.CreateForOrder(order, InvoiceType.EFatura, "INV-200");

        invoice.IsEInvoiceTaxpayer.Should().BeTrue();
    }

    [Fact]
    public void CreateForOrder_WithEArsiv_ShouldSetIsEInvoiceTaxpayerFalse()
    {
        var order = new Order
        {
            TenantId = Guid.NewGuid(),
            CustomerName = "Test Musteri"
        };

        var invoice = Invoice.CreateForOrder(order, InvoiceType.EArsiv, "INV-300");

        invoice.IsEInvoiceTaxpayer.Should().BeFalse();
    }
}
