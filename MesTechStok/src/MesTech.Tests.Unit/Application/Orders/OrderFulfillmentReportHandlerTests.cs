using FluentAssertions;
using MesTech.Application.Features.Reports.OrderFulfillmentReport;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Orders;

[Trait("Category", "Unit")]
[Trait("Domain", "Orders")]
public class OrderFulfillmentReportHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();

    private OrderFulfillmentReportHandler CreateSut() => new(_orderRepo.Object);

    private static Order CreateOrder(
        PlatformType platform,
        DateTime orderDate,
        DateTime? shippedAt,
        DateTime? deliveredAt,
        Guid tenantId)
    {
        var order = new Order
        {
            OrderNumber = "ORD-" + Guid.NewGuid().ToString()[..4],
            CustomerName = "Test",
            PaymentStatus = "Paid",
            TenantId = tenantId,
            OrderDate = orderDate,
            SourcePlatform = platform
        };
        order.SetFinancials(0m, 0m, 100m);

        // Use reflection to set private-set properties
        if (shippedAt.HasValue)
        {
            typeof(Order).GetProperty("ShippedAt")!.SetValue(order, shippedAt.Value);
            order.Status = OrderStatus.Shipped;
        }

        if (deliveredAt.HasValue)
        {
            typeof(Order).GetProperty("DeliveredAt")!.SetValue(order, deliveredAt.Value);
            order.Status = OrderStatus.Delivered;
        }

        return order;
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsGroupedReport()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var query = new OrderFulfillmentReportQuery(tenantId, startDate, endDate);

        var orders = new List<Order>
        {
            CreateOrder(PlatformType.Trendyol, startDate, startDate.AddHours(6), startDate.AddDays(2), tenantId),
            CreateOrder(PlatformType.Trendyol, startDate.AddDays(1), startDate.AddDays(1).AddHours(3), null, tenantId),
            CreateOrder(PlatformType.Hepsiburada, startDate, startDate.AddHours(12), startDate.AddDays(3), tenantId)
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                tenantId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.AsReadOnly());

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2); // Two platforms
        var trendyolReport = result.First(r => r.Platform == "Trendyol");
        trendyolReport.TotalOrders.Should().Be(2);
        trendyolReport.ShippedOrders.Should().Be(2);
        trendyolReport.DeliveredOrders.Should().Be(1);
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
    public async Task Handle_NoOrders_ReturnsEmptyReport()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new OrderFulfillmentReportQuery(
            tenantId,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc));

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_OrdersWithoutPlatform_ExcludedFromReport()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var query = new OrderFulfillmentReportQuery(tenantId, startDate, endDate);

        var orderWithPlatform = CreateOrder(PlatformType.Trendyol, startDate, startDate.AddHours(4), null, tenantId);
        var orderWithoutPlatform = new Order
        {
            OrderNumber = "ORD-NOPLAT",
            CustomerName = "Test",
            PaymentStatus = "Paid",
            TenantId = tenantId,
            OrderDate = startDate,
            SourcePlatform = null // No platform
        };
        orderWithoutPlatform.SetFinancials(0m, 0m, 100m);

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                tenantId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { orderWithPlatform, orderWithoutPlatform }.AsReadOnly());

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Platform.Should().Be("Trendyol");
        result[0].TotalOrders.Should().Be(1);
    }
}
