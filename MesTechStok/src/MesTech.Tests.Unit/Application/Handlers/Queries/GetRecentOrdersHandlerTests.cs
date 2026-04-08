using FluentAssertions;
using MesTech.Application.Features.Dashboard.Queries.GetRecentOrders;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

/// <summary>
/// DEV5: GetRecentOrdersHandler testi — dashboard son siparişler.
/// P1: Dashboard'un ana göstergesi, boş veya yanlış veri = kötü UX.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetRecentOrdersHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();

    private GetRecentOrdersHandler CreateSut() => new(_orderRepo.Object);

    private static Order MakeOrder(Guid tenantId, string orderNumber, DateTime orderDate, PlatformType? platform = null)
    {
        var order = FakeData.CreateOrder(sourcePlatform: platform);
        // Override generated fields
        order.OrderNumber = orderNumber;
        order.OrderDate = orderDate;
        // Use reflection-free approach: TenantId is settable
        order.TenantId = tenantId;
        return order;
    }

    [Fact]
    public async Task Handle_NoOrders_ShouldReturnEmpty()
    {
        _orderRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var query = new GetRecentOrdersQuery(Guid.NewGuid());
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldOrderByDateDescending()
    {
        var tenantId = Guid.NewGuid();
        var orders = new List<Order>
        {
            MakeOrder(tenantId, "O-OLD", DateTime.UtcNow.AddDays(-10)),
            MakeOrder(tenantId, "O-NEW", DateTime.UtcNow.AddDays(-1)),
            MakeOrder(tenantId, "O-MID", DateTime.UtcNow.AddDays(-5)),
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var query = new GetRecentOrdersQuery(tenantId);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.Should().HaveCount(3);
        result[0].OrderNumber.Should().Be("O-NEW");
        result[1].OrderNumber.Should().Be("O-MID");
        result[2].OrderNumber.Should().Be("O-OLD");
    }

    [Fact]
    public async Task Handle_ShouldRespectCount()
    {
        var tenantId = Guid.NewGuid();
        var orders = Enumerable.Range(1, 20).Select(i =>
            MakeOrder(tenantId, $"O-{i}", DateTime.UtcNow.AddHours(-i))
        ).ToList();

        _orderRepo.Setup(r => r.GetByDateRangeAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var query = new GetRecentOrdersQuery(tenantId, Count: 5);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_ShouldMapPlatformCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var orders = new List<Order>
        {
            MakeOrder(tenantId, "O-TR", DateTime.UtcNow, PlatformType.Trendyol),
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var query = new GetRecentOrdersQuery(tenantId);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result[0].Platform.Should().Be("Trendyol");
    }
}
