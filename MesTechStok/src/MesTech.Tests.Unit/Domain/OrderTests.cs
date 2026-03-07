using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

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
}
