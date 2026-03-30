using FluentAssertions;
using MesTech.Application.Features.Orders.Queries.GetOrderDetail;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetOrderDetailHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<ILogger<GetOrderDetailHandler>> _loggerMock = new();
    private readonly GetOrderDetailHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetOrderDetailHandlerTests()
    {
        _sut = new GetOrderDetailHandler(_orderRepoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNull()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _sut.Handle(
            new GetOrderDetailQuery(_tenantId, Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TenantMismatch_ReturnsNull()
    {
        var order = new Order { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), OrderNumber = "ORD-001" };
        _orderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _sut.Handle(
            new GetOrderDetailQuery(_tenantId, order.Id), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidOrder_ReturnsDetail()
    {
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            TenantId = _tenantId,
            OrderNumber = "ORD-100",
            Status = OrderStatus.Pending,
            CustomerName = "Test Müşteri",
            OrderDate = DateTime.UtcNow
        };
        order.SetFinancials(100m, 18m, 118m);

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _sut.Handle(
            new GetOrderDetailQuery(_tenantId, orderId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.OrderNumber.Should().Be("ORD-100");
        result.CustomerName.Should().Be("Test Müşteri");
        result.TotalAmount.Should().Be(118m);
        result.LineItems.Should().BeEmpty();
    }
}
