using FluentAssertions;
using MesTech.Application.Features.Orders.Queries.GetOrdersByStatus;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Features.Orders;

[Trait("Category", "Unit")]
public class GetOrdersByStatusHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly GetOrdersByStatusHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetOrdersByStatusHandlerTests()
        => _sut = new GetOrdersByStatusHandler(_orderRepoMock.Object);

    [Fact]
    public async Task Handle_GroupsOrdersByStatus()
    {
        var orders = new List<Order>
        {
            CreateOrder(OrderStatus.Pending, 100m),
            CreateOrder(OrderStatus.Pending, 200m),
            CreateOrder(OrderStatus.Shipped, 300m),
        };
        _orderRepoMock.Setup(r => r.GetByDateRangeAsync(
            _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var result = await _sut.Handle(new GetOrdersByStatusQuery(_tenantId), CancellationToken.None);

        result.TotalOrders.Should().Be(3);
        result.Columns.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyOrders_ReturnsZeroTotal()
    {
        _orderRepoMock.Setup(r => r.GetByDateRangeAsync(
            _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var result = await _sut.Handle(new GetOrdersByStatusQuery(_tenantId), CancellationToken.None);

        result.TotalOrders.Should().Be(0);
        result.Columns.Should().BeEmpty();
    }

    private Order CreateOrder(OrderStatus status, decimal total)
    {
        var order = Order.CreateManual(_tenantId, Guid.NewGuid(), "Test Customer", null, "SALE");
        order.SetFinancials(total, 0m, total);
        typeof(Order).GetProperty("Status")!.SetValue(order, status);
        return order;
    }
}
