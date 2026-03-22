using FluentAssertions;
using MesTech.Application.Queries.ListOrders;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class ListOrdersHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();

    private static Order CreateOrder(string orderNumber, OrderStatus status, DateTime? date = null)
    {
        var order = new Order
        {
            OrderNumber = orderNumber,
            Status = status,
            OrderDate = date ?? DateTime.UtcNow,
            CustomerName = "Test Musteri",
            PaymentStatus = "Paid",
            TenantId = Guid.NewGuid()
        };
        order.SetFinancials(0m, 0m, 100m);
        return order;
    }

    [Fact]
    public async Task Handle_NoFilters_UsesLast30DaysRange()
    {
        _orderRepo
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var handler = new ListOrdersHandler(_orderRepo.Object);
        var result = await handler.Handle(new ListOrdersQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        _orderRepo.Verify(r => r.GetByDateRangeAsync(
            It.Is<DateTime>(d => d < DateTime.UtcNow.AddDays(-29)),
            It.Is<DateTime>(d => d >= DateTime.UtcNow.AddMinutes(-1))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDateRange_PassesDatesToRepository()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to   = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        _orderRepo
            .Setup(r => r.GetByDateRangeAsync(from, to))
            .ReturnsAsync(new List<Order> { CreateOrder("ORD-0001", OrderStatus.Confirmed, from) }.AsReadOnly());

        var handler = new ListOrdersHandler(_orderRepo.Object);
        var result  = await handler.Handle(new ListOrdersQuery(from, to), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].OrderNumber.Should().Be("ORD-0001");
        _orderRepo.Verify(r => r.GetByDateRangeAsync(from, to), Times.Once);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ReturnsOnlyMatchingOrders()
    {
        var orders = new List<Order>
        {
            CreateOrder("ORD-001", OrderStatus.Confirmed),
            CreateOrder("ORD-002", OrderStatus.Pending),
            CreateOrder("ORD-003", OrderStatus.Confirmed)
        }.AsReadOnly();

        _orderRepo
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(orders);

        var handler = new ListOrdersHandler(_orderRepo.Object);
        var result  = await handler.Handle(
            new ListOrdersQuery(Status: "Confirmed"), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o => o.Status.Should().Be("Confirmed"));
    }

    [Fact]
    public async Task Handle_StatusFilterCaseInsensitive_Works()
    {
        var orders = new List<Order>
        {
            CreateOrder("ORD-001", OrderStatus.Pending)
        }.AsReadOnly();

        _orderRepo
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(orders);

        var handler = new ListOrdersHandler(_orderRepo.Object);
        var result  = await handler.Handle(
            new ListOrdersQuery(Status: "pending"), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NoMatchingStatus_ReturnsEmpty()
    {
        var orders = new List<Order>
        {
            CreateOrder("ORD-001", OrderStatus.Confirmed)
        }.AsReadOnly();

        _orderRepo
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(orders);

        var handler = new ListOrdersHandler(_orderRepo.Object);
        var result  = await handler.Handle(
            new ListOrdersQuery(Status: "Cancelled"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmpty()
    {
        _orderRepo
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var handler = new ListOrdersHandler(_orderRepo.Object);
        var result  = await handler.Handle(new ListOrdersQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
