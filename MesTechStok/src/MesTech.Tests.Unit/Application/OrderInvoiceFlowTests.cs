using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Tests.Unit.Application;

/// <summary>
/// Siparis -> Fatura domain akis testleri.
/// Order.Place() sonrasi Invoice olusturma ve durum gecisleri dogrulanir.
/// </summary>
[Trait("Category", "Unit")]
public class OrderInvoiceFlowTests
{
    private static Order CreateSampleOrder(string orderNumber = "ORD-TEST-001")
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var order = new Order
        {
            TenantId = tenantId,
            OrderNumber = orderNumber,
            CustomerId = customerId,
            CustomerName = "Test Musteri",
            CustomerEmail = "test@example.com",
            Status = OrderStatus.Pending
        };

        var item1 = new OrderItem
        {
            TenantId = tenantId,
            ProductId = Guid.NewGuid(),
            ProductName = "Urun A",
            ProductSKU = "SKU-001",
            Quantity = 2,
            UnitPrice = 100m,
            TotalPrice = 200m,
            TaxRate = 0.18m,
            TaxAmount = 36m
        };

        var item2 = new OrderItem
        {
            TenantId = tenantId,
            ProductId = Guid.NewGuid(),
            ProductName = "Urun B",
            ProductSKU = "SKU-002",
            Quantity = 1,
            UnitPrice = 50m,
            TotalPrice = 50m,
            TaxRate = 0.18m,
            TaxAmount = 9m
        };

        order.AddItem(item1);
        order.AddItem(item2);

        return order;
    }

    [Fact]
    public void Place_ShouldRaise_OrderPlacedEvent()
    {
        // Arrange
        var order = CreateSampleOrder();

        // Act
        order.Place();

        // Assert
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderPlacedEvent>();

        var evt = (OrderPlacedEvent)order.DomainEvents[0];
        evt.OrderId.Should().Be(order.Id);
        evt.OrderNumber.Should().Be("ORD-TEST-001");
        evt.TotalAmount.Should().Be(order.TotalAmount);
    }

    [Fact]
    public void CreateForOrder_ShouldCreateCorrectInvoice_FromOrder()
    {
        // Arrange
        var order = CreateSampleOrder();
        order.Place();

        // Act
        var invoice = Invoice.CreateForOrder(order, InvoiceType.EFatura, "INV-001");

        // Assert
        invoice.OrderId.Should().Be(order.Id);
        invoice.TenantId.Should().Be(order.TenantId);
        invoice.InvoiceNumber.Should().Be("INV-001");
        invoice.Type.Should().Be(InvoiceType.EFatura);
        invoice.CustomerName.Should().Be("Test Musteri");
        invoice.CustomerEmail.Should().Be("test@example.com");
        invoice.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public void CreateForOrder_ShouldHaveCorrectAmounts()
    {
        // Arrange
        var order = CreateSampleOrder();
        order.Place();

        // Act
        var invoice = Invoice.CreateForOrder(order, InvoiceType.EArsiv, "INV-002");

        // Assert
        invoice.SubTotal.Should().Be(order.SubTotal);
        invoice.TaxTotal.Should().Be(order.TaxAmount);
        invoice.GrandTotal.Should().Be(order.TotalAmount);
        invoice.GrandTotal.Should().Be(295m); // 250 + 45
    }

    [Fact]
    public void CreateForOrder_EFatura_ShouldHave_IsEInvoiceTaxpayer_True()
    {
        // Arrange
        var order = CreateSampleOrder();
        order.Place();

        // Act
        var invoice = Invoice.CreateForOrder(order, InvoiceType.EFatura, "INV-003");

        // Assert
        invoice.IsEInvoiceTaxpayer.Should().BeTrue();
        invoice.Type.Should().Be(InvoiceType.EFatura);
    }

    [Fact]
    public void CreateForOrder_NonTaxpayer_ShouldUseEArsivType()
    {
        // Arrange
        var order = CreateSampleOrder();
        order.Place();

        // Act
        var invoice = Invoice.CreateForOrder(order, InvoiceType.EArsiv, "INV-004");

        // Assert
        invoice.IsEInvoiceTaxpayer.Should().BeFalse();
        invoice.Type.Should().Be(InvoiceType.EArsiv);
    }

    [Fact]
    public void MarkAsSent_AfterCreation_ShouldProgressFromDraftToSent()
    {
        // Arrange
        var order = CreateSampleOrder();
        order.Place();
        var invoice = Invoice.CreateForOrder(order, InvoiceType.EFatura, "INV-005");

        invoice.Status.Should().Be(InvoiceStatus.Draft);

        // Act
        invoice.MarkAsSent("GIB-12345", "https://cdn.example.com/invoice.pdf");

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Sent);
        invoice.GibInvoiceId.Should().Be("GIB-12345");
        invoice.PdfUrl.Should().Be("https://cdn.example.com/invoice.pdf");
        invoice.SentAt.Should().NotBeNull();
        invoice.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // InvoiceSentEvent should be raised (in addition to InvoiceCreatedEvent)
        invoice.DomainEvents.Should().HaveCount(2);
        invoice.DomainEvents[1].Should().BeOfType<InvoiceSentEvent>();
    }

    [Fact]
    public void FullFlow_Order_Place_Invoice_MarkAsSent_MarkAsAccepted()
    {
        // Arrange
        var order = CreateSampleOrder("ORD-FLOW-001");

        // Act & Assert: Step 1 — Place Order
        order.Place();
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderPlacedEvent>();

        // Act & Assert: Step 2 — Create Invoice
        var invoice = Invoice.CreateForOrder(order, InvoiceType.EFatura, "INV-FLOW-001");
        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.GrandTotal.Should().Be(order.TotalAmount);
        invoice.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<InvoiceCreatedEvent>();

        // Act & Assert: Step 3 — Mark as Sent
        invoice.MarkAsSent("GIB-FLOW-001", "https://cdn.example.com/flow.pdf");
        invoice.Status.Should().Be(InvoiceStatus.Sent);
        invoice.DomainEvents.Should().HaveCount(2);

        // Act & Assert: Step 4 — Mark as Accepted
        invoice.MarkAsAccepted();
        invoice.Status.Should().Be(InvoiceStatus.Accepted);
        invoice.AcceptedAt.Should().NotBeNull();
        invoice.AcceptedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void FullFlow_Order_Place_Invoice_Cancel_ShouldWorkFromDraft()
    {
        // Arrange
        var order = CreateSampleOrder("ORD-CANCEL-001");

        // Act: Place Order
        order.Place();
        order.Status.Should().Be(OrderStatus.Confirmed);

        // Act: Create Invoice
        var invoice = Invoice.CreateForOrder(order, InvoiceType.EArsiv, "INV-CANCEL-001");
        invoice.Status.Should().Be(InvoiceStatus.Draft);

        // Act: Cancel from Draft
        invoice.Cancel();

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Cancelled);

        // Verify that cancelling an Accepted invoice throws
        var acceptedInvoice = Invoice.CreateForOrder(order, InvoiceType.EFatura, "INV-CANCEL-002");
        acceptedInvoice.MarkAsSent("GIB-X", null);
        acceptedInvoice.MarkAsAccepted();

        var act = () => acceptedInvoice.Cancel();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*iptal*");
    }
}
