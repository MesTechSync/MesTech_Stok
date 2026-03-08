using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// Order entity domain logic koruma testleri.
/// Bu testler kirilirsa = siparis mantigi bozulmus demektir.
/// </summary>
[Trait("Category", "Unit")]
public class OrderTests
{
    [Fact]
    public void PlaceOrder_ShouldSetStatusToConfirmed()
    {
        var order = new Order
        {
            OrderNumber = "ORD-001",
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Pending
        };

        order.Place();

        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void PlaceOrder_ShouldRaiseDomainEvent()
    {
        var order = new Order
        {
            OrderNumber = "ORD-002",
            CustomerId = Guid.NewGuid()
        };

        order.Place();

        order.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void OrderStatus_Flow_ShouldFollowStateMachine()
    {
        var order = new Order { Status = OrderStatus.Pending };

        // Pending -> Confirmed
        order.Status = OrderStatus.Confirmed;
        order.Status.Should().Be(OrderStatus.Confirmed);

        // Confirmed -> Shipped
        order.Status = OrderStatus.Shipped;
        order.Status.Should().Be(OrderStatus.Shipped);

        // Shipped -> Delivered
        order.Status = OrderStatus.Delivered;
        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public void CancelOrder_ShouldSetStatusToCancelled()
    {
        var order = new Order { Status = OrderStatus.Pending };

        order.Status = OrderStatus.Cancelled;

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Order_DefaultStatus_ShouldBePending()
    {
        var order = new Order();

        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void Order_ShouldTrackCreationDate()
    {
        var order = new Order();

        order.OrderDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsShipped_FromConfirmed_ShouldSetShippedStatus()
    {
        var order = new Order { Status = OrderStatus.Confirmed };

        order.MarkAsShipped("YK123456789", MesTech.Domain.Enums.CargoProvider.YurticiKargo);

        order.Status.Should().Be(OrderStatus.Shipped);
        order.TrackingNumber.Should().Be("YK123456789");
        order.CargoProvider.Should().Be(MesTech.Domain.Enums.CargoProvider.YurticiKargo);
        order.ShippedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsShipped_ShouldRaiseOrderShippedEvent()
    {
        var order = new Order { Status = OrderStatus.Confirmed };

        order.MarkAsShipped("AR987654321", MesTech.Domain.Enums.CargoProvider.ArasKargo);

        order.DomainEvents.Should().ContainSingle();
        order.DomainEvents.First().Should().BeOfType<MesTech.Domain.Events.OrderShippedEvent>();
    }

    [Fact]
    public void MarkAsShipped_FromPending_ShouldThrow()
    {
        var order = new Order { Status = OrderStatus.Pending };

        var act = () => order.MarkAsShipped("YK123", MesTech.Domain.Enums.CargoProvider.YurticiKargo);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot ship order in Pending status*");
    }

    [Fact]
    public void MarkAsShipped_FromDelivered_ShouldThrow()
    {
        var order = new Order { Status = OrderStatus.Delivered };

        var act = () => order.MarkAsShipped("YK123", MesTech.Domain.Enums.CargoProvider.YurticiKargo);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot ship order in Delivered status*");
    }

    [Fact]
    public void Order_PlatformFields_ShouldDefaultToNull()
    {
        var order = new Order();

        order.SourcePlatform.Should().BeNull();
        order.ExternalOrderId.Should().BeNull();
        order.CargoProvider.Should().BeNull();
        order.TrackingNumber.Should().BeNull();
        order.AutoShipmentEnabled.Should().BeFalse();
    }
}
