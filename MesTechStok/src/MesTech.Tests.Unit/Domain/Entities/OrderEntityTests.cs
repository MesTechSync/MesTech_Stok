using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Exceptions;

namespace MesTech.Tests.Unit.Domain.Entities;

/// <summary>
/// Order entity domain behavior tests.
/// Status transitions, AddItem, CalculateTotals, factory, domain events.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "OrderEntity")]
[Trait("Phase", "Dalga15")]
public class OrderEntityTests
{
    private static Order CreateOrder(OrderStatus status = OrderStatus.Pending)
    {
        var order = new Order
        {
            TenantId = Guid.NewGuid(),
            OrderNumber = "ORD-20260325-001",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            CustomerEmail = "test@example.com",
            Type = "SALE"
        };
        // Set status via domain methods for valid transitions
        if (status == OrderStatus.Confirmed)
            order.Place();
        else if (status == OrderStatus.Shipped)
        {
            order.Place();
            order.MarkAsShipped("TRK-001", CargoProvider.YurticiKargo);
        }
        else if (status == OrderStatus.Delivered)
        {
            order.Place();
            order.MarkAsShipped("TRK-001", CargoProvider.YurticiKargo);
            order.MarkAsDelivered();
        }
        return order;
    }

    private static OrderItem CreateOrderItem(decimal unitPrice = 100m, int quantity = 2, decimal taxRate = 0.18m)
    {
        var item = new OrderItem
        {
            TenantId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Item",
            ProductSKU = "ITEM-001",
            TaxRate = taxRate
        };
        item.SetQuantityAndPrice(quantity, unitPrice);
        return item;
    }

    // ═══════════════════════════════════════════
    // Order Creation Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void NewOrder_HasPendingStatus()
    {
        var order = new Order { OrderNumber = "ORD-001" };
        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void NewOrder_HasPendingPaymentStatus()
    {
        var order = new Order();
        order.PaymentStatus.Should().Be("Pending");
    }

    [Fact]
    public void NewOrder_HasSaleType()
    {
        var order = new Order();
        order.Type.Should().Be("SALE");
    }

    // ═══════════════════════════════════════════
    // Status Transition: Pending -> Confirmed (Place)
    // ═══════════════════════════════════════════

    [Fact]
    public void Place_FromPending_SetsConfirmedStatus()
    {
        var order = CreateOrder(OrderStatus.Pending);
        order.ClearDomainEvents();

        order.Place();

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public void Place_RaisesOrderPlacedEvent()
    {
        var order = CreateOrder(OrderStatus.Pending);
        order.ClearDomainEvents();

        order.Place();

        order.DomainEvents.Should().ContainSingle(e => e is OrderPlacedEvent);
    }

    [Fact]
    public void Place_FromConfirmed_ThrowsBusinessRuleException()
    {
        var order = CreateOrder(OrderStatus.Confirmed);

        var act = () => order.Place();

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Place_FromShipped_ThrowsBusinessRuleException()
    {
        var order = CreateOrder(OrderStatus.Shipped);

        var act = () => order.Place();

        act.Should().Throw<BusinessRuleException>();
    }

    // ═══════════════════════════════════════════
    // Status Transition: Confirmed -> Shipped
    // ═══════════════════════════════════════════

    [Fact]
    public void MarkAsShipped_FromConfirmed_SetsShippedStatus()
    {
        var order = CreateOrder(OrderStatus.Confirmed);
        order.ClearDomainEvents();

        order.MarkAsShipped("TRK-999", CargoProvider.ArasKargo);

        order.Status.Should().Be(OrderStatus.Shipped);
        order.TrackingNumber.Should().Be("TRK-999");
        order.CargoProvider.Should().Be(CargoProvider.ArasKargo);
        order.ShippedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsShipped_RaisesOrderShippedEvent()
    {
        var order = CreateOrder(OrderStatus.Confirmed);
        order.ClearDomainEvents();

        order.MarkAsShipped("TRK-999", CargoProvider.ArasKargo);

        order.DomainEvents.Should().ContainSingle(e => e is OrderShippedEvent);
    }

    [Fact]
    public void MarkAsShipped_FromPending_ThrowsBusinessRuleException()
    {
        var order = CreateOrder(OrderStatus.Pending);

        var act = () => order.MarkAsShipped("TRK-999", CargoProvider.ArasKargo);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void MarkAsShipped_FromDelivered_ThrowsBusinessRuleException()
    {
        var order = CreateOrder(OrderStatus.Delivered);

        var act = () => order.MarkAsShipped("TRK-999", CargoProvider.ArasKargo);

        act.Should().Throw<BusinessRuleException>();
    }

    // ═══════════════════════════════════════════
    // Status Transition: Shipped -> Delivered
    // ═══════════════════════════════════════════

    [Fact]
    public void MarkAsDelivered_FromShipped_SetsDeliveredStatus()
    {
        var order = CreateOrder(OrderStatus.Shipped);
        order.ClearDomainEvents();

        order.MarkAsDelivered();

        order.Status.Should().Be(OrderStatus.Delivered);
        order.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsDelivered_RaisesOrderReceivedEvent()
    {
        var order = CreateOrder(OrderStatus.Shipped);
        order.ClearDomainEvents();

        order.MarkAsDelivered();

        order.DomainEvents.Should().ContainSingle(e => e is OrderReceivedEvent);
    }

    [Fact]
    public void MarkAsDelivered_FromPending_ThrowsBusinessRuleException()
    {
        var order = CreateOrder(OrderStatus.Pending);

        var act = () => order.MarkAsDelivered();

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void MarkAsDelivered_FromConfirmed_ThrowsBusinessRuleException()
    {
        var order = CreateOrder(OrderStatus.Confirmed);

        var act = () => order.MarkAsDelivered();

        act.Should().Throw<BusinessRuleException>();
    }

    // ═══════════════════════════════════════════
    // Cancel Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void Cancel_FromPending_SetsCancelledStatus()
    {
        var order = CreateOrder(OrderStatus.Pending);

        order.Cancel("customer request");

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromConfirmed_SetsCancelledStatus()
    {
        var order = CreateOrder(OrderStatus.Confirmed);
        order.ClearDomainEvents();

        order.Cancel("out of stock");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.Should().ContainSingle(e => e is OrderCancelledEvent);
    }

    [Fact]
    public void Cancel_FromShipped_ThrowsBusinessRuleException()
    {
        var order = CreateOrder(OrderStatus.Shipped);

        var act = () => order.Cancel("too late");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Cancel_FromDelivered_ThrowsBusinessRuleException()
    {
        var order = CreateOrder(OrderStatus.Delivered);

        var act = () => order.Cancel();

        act.Should().Throw<BusinessRuleException>();
    }

    // ═══════════════════════════════════════════
    // AddItem / CalculateTotals Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void AddItem_CalculatesTotalsAutomatically()
    {
        var order = new Order { TenantId = Guid.NewGuid(), OrderNumber = "ORD-CALC" };
        var item = CreateOrderItem(unitPrice: 100m, quantity: 3, taxRate: 0.18m);

        order.AddItem(item);

        order.SubTotal.Should().Be(300m);
        order.TaxAmount.Should().Be(54m);
        order.TotalAmount.Should().Be(354m);
    }

    [Fact]
    public void AddItem_MultipleItems_SumsCorrectly()
    {
        var order = new Order { TenantId = Guid.NewGuid(), OrderNumber = "ORD-MULTI" };
        var item1 = CreateOrderItem(unitPrice: 100m, quantity: 2, taxRate: 0.18m);
        var item2 = CreateOrderItem(unitPrice: 50m, quantity: 1, taxRate: 0.08m);

        order.AddItem(item1);
        order.AddItem(item2);

        order.SubTotal.Should().Be(250m); // (100*2) + (50*1)
        order.TaxAmount.Should().Be(40m); // (200*0.18) + (50*0.08)
        order.TotalAmount.Should().Be(290m);
    }

    [Fact]
    public void TotalItems_SumsQuantities()
    {
        var order = new Order { TenantId = Guid.NewGuid(), OrderNumber = "ORD-QTY" };
        order.AddItem(CreateOrderItem(quantity: 3));
        order.AddItem(CreateOrderItem(quantity: 5));

        order.TotalItems.Should().Be(8);
    }

    // ═══════════════════════════════════════════
    // OrderItem Validation Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void OrderItem_SetQuantityAndPrice_ZeroQuantity_Throws()
    {
        var item = new OrderItem();

        var act = () => item.SetQuantityAndPrice(0, 10m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OrderItem_SetQuantityAndPrice_NegativeUnitPrice_Throws()
    {
        var item = new OrderItem();

        var act = () => item.SetQuantityAndPrice(1, -5m);

        act.Should().Throw<ArgumentException>();
    }

    // ═══════════════════════════════════════════
    // Financial & Commission Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void SetFinancials_OverridesTotals()
    {
        var order = new Order { TenantId = Guid.NewGuid() };

        order.SetFinancials(500m, 90m, 590m);

        order.SubTotal.Should().Be(500m);
        order.TaxAmount.Should().Be(90m);
        order.TotalAmount.Should().Be(590m);
    }

    [Fact]
    public void SetCommission_SetsRateAndAmount()
    {
        var order = new Order();

        order.SetCommission(0.15m, 75m);

        order.CommissionRate.Should().Be(0.15m);
        order.CommissionAmount.Should().Be(75m);
    }

    [Fact]
    public void MarkAsPaid_SetsPaymentStatusToPaid()
    {
        var order = new Order();

        order.MarkAsPaid();

        order.PaymentStatus.Should().Be("Paid");
    }

    [Fact]
    public void SetCargoExpense_SetsAmount()
    {
        var order = new Order();

        order.SetCargoExpense(29.90m);

        order.CargoExpenseAmount.Should().Be(29.90m);
    }

    [Fact]
    public void SetCargoBarcode_SetsBarcode()
    {
        var order = new Order();

        order.SetCargoBarcode("CARGO-BC-123");

        order.CargoBarcode.Should().Be("CARGO-BC-123");
    }

    // ═══════════════════════════════════════════
    // Factory Method Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void CreateFromPlatform_SetsAllFields()
    {
        var tenantId = Guid.NewGuid();
        var items = new List<OrderItem> { CreateOrderItem() };

        var order = Order.CreateFromPlatform(
            tenantId, "TY-EXT-001", PlatformType.Trendyol,
            "Ali Veli", "ali@test.com", items);

        order.TenantId.Should().Be(tenantId);
        order.ExternalOrderId.Should().Be("TY-EXT-001");
        order.SourcePlatform.Should().Be(PlatformType.Trendyol);
        order.CustomerName.Should().Be("Ali Veli");
        order.Status.Should().Be(OrderStatus.Pending);
        order.OrderItems.Should().HaveCount(1);
        order.OrderNumber.Should().StartWith("TR-");
    }

    [Fact]
    public void ToString_ContainsOrderNumberAndStatus()
    {
        var order = new Order { OrderNumber = "ORD-TEST" };

        order.ToString().Should().Contain("ORD-TEST");
        order.ToString().Should().Contain("Pending");
    }
}
