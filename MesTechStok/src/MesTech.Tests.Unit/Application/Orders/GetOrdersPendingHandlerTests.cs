using FluentAssertions;
using MesTech.Application.Features.Dashboard.Queries.GetOrdersPending;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Orders;

[Trait("Category", "Unit")]
[Trait("Domain", "Orders")]
public class GetOrdersPendingHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();

    private GetOrdersPendingHandler CreateSut() => new(_orderRepo.Object);

    private static Order CreateOrder(OrderStatus status, DateTime orderDate, Guid tenantId)
    {
        var order = new Order
        {
            OrderNumber = "ORD-" + Guid.NewGuid().ToString()[..4],
            CustomerName = "Test",
            PaymentStatus = "Paid",
            TenantId = tenantId,
            OrderDate = orderDate,
            Status = status
        };
        order.SetFinancials(0m, 0m, 100m);
        return order;
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsPendingOrderCounts()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new GetOrdersPendingQuery(tenantId);

        var orders = new List<Order>
        {
            CreateOrder(OrderStatus.Pending, DateTime.UtcNow.AddHours(-2), tenantId),
            CreateOrder(OrderStatus.Confirmed, DateTime.UtcNow.AddHours(-1), tenantId),
            CreateOrder(OrderStatus.Shipped, DateTime.UtcNow.AddHours(-3), tenantId) // Not pending
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                tenantId,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.AsReadOnly());

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.Should().Be(2); // Pending + Confirmed
        result.Urgent.Should().Be(0); // None older than 24 hours
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_UrgentOrders_CountsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new GetOrdersPendingQuery(tenantId);

        var orders = new List<Order>
        {
            CreateOrder(OrderStatus.Pending, DateTime.UtcNow.AddHours(-25), tenantId), // Urgent (>24h)
            CreateOrder(OrderStatus.Pending, DateTime.UtcNow.AddHours(-48), tenantId), // Urgent (>24h)
            CreateOrder(OrderStatus.Confirmed, DateTime.UtcNow.AddHours(-2), tenantId)  // Not urgent
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                tenantId,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.AsReadOnly());

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.Should().Be(3);
        result.Urgent.Should().Be(2);
        result.OldestMinutes.Should().BeGreaterThan(48 * 60 - 5); // ~48 hours in minutes
    }

    [Fact]
    public async Task Handle_NoPendingOrders_ReturnsZeroCounts()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new GetOrdersPendingQuery(tenantId);

        var orders = new List<Order>
        {
            CreateOrder(OrderStatus.Shipped, DateTime.UtcNow.AddHours(-5), tenantId),
            CreateOrder(OrderStatus.Delivered, DateTime.UtcNow.AddDays(-1), tenantId)
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                tenantId,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.AsReadOnly());

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.Should().Be(0);
        result.Urgent.Should().Be(0);
        result.OldestMinutes.Should().Be(0);
    }
}
