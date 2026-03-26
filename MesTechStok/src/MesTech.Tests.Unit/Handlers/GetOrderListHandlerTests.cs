using FluentAssertions;
using MesTech.Application.Features.Orders.Queries.GetOrderList;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetOrderListHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly GetOrderListHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetOrderListHandlerTests()
    {
        _sut = new GetOrderListHandler(_orderRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsOrderList()
    {
        var orders = new List<Order>
        {
            new() { Id = Guid.NewGuid(), OrderNumber = "ORD-001", CustomerName = "Müşteri A", Status = OrderStatus.Pending, TotalAmount = 100m, OrderDate = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), OrderNumber = "ORD-002", CustomerName = "Müşteri B", Status = OrderStatus.Confirmed, TotalAmount = 250m, OrderDate = DateTime.UtcNow.AddDays(-1) }
        };

        _orderRepoMock.Setup(r => r.GetRecentAsync(_tenantId, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.AsReadOnly());

        var query = new GetOrderListQuery(_tenantId, 100);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].OrderNumber.Should().Be("ORD-001");
        result[1].CustomerName.Should().Be("Müşteri B");
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsEmpty()
    {
        _orderRepoMock.Setup(r => r.GetRecentAsync(_tenantId, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var query = new GetOrderListQuery(_tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
