using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.EdgeCases;

/// <summary>
/// Order entity edge case tests — boundary values, null inputs, state transitions.
/// Dalga 14 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class OrderEdgeCaseTests
{
    [Fact]
    public void AddItem_SingleItem_ShouldRecalculateTotals()
    {
        var order = new Order();
        var item = new OrderItem
        {
            ProductName = "Test",
            Quantity = 2,
            UnitPrice = 100m,
            TotalPrice = 200m,
            TaxRate = 0.20m,
            TaxAmount = 40m
        };

        order.AddItem(item);

        order.SubTotal.Should().Be(200m);
        order.TaxAmount.Should().Be(40m);
        order.TotalAmount.Should().Be(240m);
    }

    [Fact]
    public void AddItem_MultipleItems_ShouldAccumulateTotals()
    {
        var order = new Order();

        order.AddItem(new OrderItem { TotalPrice = 100m, TaxAmount = 18m });
        order.AddItem(new OrderItem { TotalPrice = 200m, TaxAmount = 36m });

        order.SubTotal.Should().Be(300m);
        order.TaxAmount.Should().Be(54m);
        order.TotalAmount.Should().Be(354m);
    }

    [Fact]
    public void AddItem_ZeroPriceItem_ShouldNotChangeTotal()
    {
        var order = new Order();
        order.AddItem(new OrderItem { TotalPrice = 0m, TaxAmount = 0m });

        order.SubTotal.Should().Be(0m);
        order.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public void TotalItems_ShouldSumQuantities()
    {
        var order = new Order();
        order.AddItem(new OrderItem { Quantity = 3, TotalPrice = 0m, TaxAmount = 0m });
        order.AddItem(new OrderItem { Quantity = 5, TotalPrice = 0m, TaxAmount = 0m });

        order.TotalItems.Should().Be(8);
    }

    [Fact]
    public void TotalItems_EmptyOrder_ShouldBeZero()
    {
        var order = new Order();
        order.TotalItems.Should().Be(0);
    }

    [Fact]
    public void MarkAsShipped_FromCancelled_ShouldThrow()
    {
        var order = new Order { Status = OrderStatus.Cancelled };

        var act = () => order.MarkAsShipped("TRK-001", CargoProvider.SuratKargo);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot ship order in Cancelled status*");
    }

    [Fact]
    public void MarkAsShipped_FromShipped_ShouldThrow()
    {
        var order = new Order { Status = OrderStatus.Shipped };

        var act = () => order.MarkAsShipped("TRK-002", CargoProvider.MngKargo);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot ship order in Shipped status*");
    }

    [Fact]
    public void Place_ShouldRaiseOrderPlacedEvent_WithCorrectData()
    {
        var customerId = Guid.NewGuid();
        var order = new Order
        {
            OrderNumber = "ORD-EDGE-01",
            CustomerId = customerId
        };
        order.SetFinancials(0m, 0m, 999.99m);

        order.Place();

        order.DomainEvents.Should().ContainSingle();
        var evt = order.DomainEvents[0] as OrderPlacedEvent;
        evt.Should().NotBeNull();
        evt!.OrderNumber.Should().Be("ORD-EDGE-01");
        evt.CustomerId.Should().Be(customerId);
        evt.TotalAmount.Should().Be(999.99m);
    }

    [Fact]
    public void MarkAsShipped_ShouldRaiseEvent_WithProviderInfo()
    {
        var order = new Order { Status = OrderStatus.Confirmed };

        order.MarkAsShipped("PTT-12345", CargoProvider.PttKargo);

        var evt = order.DomainEvents[0] as OrderShippedEvent;
        evt.Should().NotBeNull();
        evt!.TrackingNumber.Should().Be("PTT-12345");
        evt.CargoProvider.Should().Be(CargoProvider.PttKargo);
    }

    [Fact]
    public void Order_DefaultType_ShouldBeSale()
    {
        var order = new Order();
        order.Type.Should().Be("SALE");
    }

    [Fact]
    public void Order_DefaultPaymentStatus_ShouldBePending()
    {
        var order = new Order();
        order.PaymentStatus.Should().Be("Pending");
    }

    [Fact]
    public void ToString_ShouldContainOrderNumberAndStatus()
    {
        var order = new Order
        {
            OrderNumber = "ORD-TS-01",
            Status = OrderStatus.Confirmed
        };
        order.SetFinancials(0m, 0m, 150m);

        var str = order.ToString();

        str.Should().Contain("ORD-TS-01");
        str.Should().Contain("Confirmed");
    }

    [Fact]
    public void OrderItems_ShouldBeReadOnlyCollection()
    {
        var order = new Order();
        order.OrderItems.Should().BeEmpty();
        order.OrderItems.Should().BeAssignableTo<IReadOnlyCollection<OrderItem>>();
    }

    [Fact]
    public void ClearDomainEvents_AfterPlace_ShouldClearEvents()
    {
        var order = new Order { OrderNumber = "ORD-CLR", CustomerId = Guid.NewGuid() };
        order.Place();
        order.DomainEvents.Should().HaveCount(1);

        order.ClearDomainEvents();

        order.DomainEvents.Should().BeEmpty();
    }
}
