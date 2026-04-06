using FluentAssertions;
using MesTech.Application.Features.Reports.SalesAnalytics;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Features.Reports;

[Trait("Category", "Unit")]
public class GetSalesAnalyticsHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly GetSalesAnalyticsHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetSalesAnalyticsHandlerTests()
        => _sut = new GetSalesAnalyticsHandler(_orderRepoMock.Object);

    [Fact]
    public async Task Handle_EmptyOrders_ReturnsDefaultDto()
    {
        _orderRepoMock.Setup(r => r.GetByDateRangeWithItemsAsync(
            _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var query = new GetSalesAnalyticsQuery(_tenantId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.TotalOrders.Should().Be(0);
        result.TotalRevenue.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithOrders_CalculatesCorrectMetrics()
    {
        var orders = new List<Order>
        {
            CreateOrderWithItems(_tenantId, 500m, 2),
            CreateOrderWithItems(_tenantId, 300m, 1),
        };
        _orderRepoMock.Setup(r => r.GetByDateRangeWithItemsAsync(
            _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var query = new GetSalesAnalyticsQuery(_tenantId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.TotalOrders.Should().Be(2);
        result.TotalRevenue.Should().Be(800m);
        result.AverageOrderValue.Should().Be(400m);
    }

    private static Order CreateOrderWithItems(Guid tenantId, decimal total, int itemCount)
    {
        var order = Order.CreateManual(tenantId, Guid.NewGuid(), "Customer", null, "SALE");
        order.SetFinancials(total, 0m, total);
        return order;
    }
}
