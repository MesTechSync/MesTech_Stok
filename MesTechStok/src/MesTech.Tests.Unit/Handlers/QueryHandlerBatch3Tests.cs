using FluentAssertions;
using MesTech.Application.Features.Dashboard.Queries.GetRevenueChart;
using MesTech.Application.Features.Dashboard.Queries.GetSalesToday;
using MesTech.Application.Features.Tenant.Queries.GetTenant;
using MesTech.Application.Features.Tenant.Queries.GetTenants;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Application.Queries.GetSupplierById;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Batch 3 — GetTenant, GetSalesToday, GetRevenueChart, GetInventoryStatistics
/// </summary>
[Trait("Category", "Unit")]
public class QueryHandlerBatch3Tests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════════════════════════════════════════════════════════
    // GetTenantHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetTenant_NotFound_ReturnsNull()
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var sut = new GetTenantHandler(repo.Object);
        var result = await sut.Handle(new GetTenantQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTenant_Found_MapsToDto()
    {
        var tenant = new Tenant
        {
            Id = _tenantId, Name = "MesTech", TaxNumber = "1234567890", IsActive = true
        };
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var sut = new GetTenantHandler(repo.Object);
        var result = await sut.Handle(new GetTenantQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("MesTech");
        result.TaxNumber.Should().Be("1234567890");
        result.IsActive.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════
    // GetSalesTodayHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSalesToday_NoOrders_ReturnsZeros()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var sut = new GetSalesTodayHandler(repo.Object);
        var result = await sut.Handle(new GetSalesTodayQuery(_tenantId), CancellationToken.None);

        result.Today.Should().Be(0);
        result.Yesterday.Should().Be(0);
        result.ChangePercent.Should().Be(0);
    }

    [Fact]
    public async Task GetSalesToday_TodayOnly_Returns100Percent()
    {
        var todayStart = DateTime.UtcNow.Date;
        var repo = new Mock<IOrderRepository>();

        // Today has orders
        repo.Setup(r => r.GetByDateRangeAsync(
                _tenantId,
                It.Is<DateTime>(d => d.Date == todayStart),
                It.Is<DateTime>(d => d.Date == todayStart.AddDays(1)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { CreateOrder(500m) });

        // Yesterday has no orders
        repo.Setup(r => r.GetByDateRangeAsync(
                _tenantId,
                It.Is<DateTime>(d => d.Date == todayStart.AddDays(-1)),
                It.Is<DateTime>(d => d.Date == todayStart),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var sut = new GetSalesTodayHandler(repo.Object);
        var result = await sut.Handle(new GetSalesTodayQuery(_tenantId), CancellationToken.None);

        result.Today.Should().Be(500m);
        result.ChangePercent.Should().Be(100m);
    }

    // ═══════════════════════════════════════════════════════════
    // GetRevenueChartHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetRevenueChart_NoOrders_ReturnsZeroFilledDays()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var sut = new GetRevenueChartHandler(repo.Object);
        var result = await sut.Handle(
            new GetRevenueChartQuery(_tenantId, Days: 7), CancellationToken.None);

        result.Should().HaveCount(7);
        result.Should().AllSatisfy(p =>
        {
            p.Revenue.Should().Be(0);
            p.OrderCount.Should().Be(0);
        });
    }

    [Fact]
    public async Task GetRevenueChart_WithOrders_GroupsByDay()
    {
        // Handler uses from = UtcNow.AddDays(-days).Date, iterates from..from+days-1
        var from = DateTime.UtcNow.AddDays(-3).Date;
        var targetDate = from; // first day in range
        var order1 = CreateOrder(200m);
        order1.OrderDate = targetDate;
        var order2 = CreateOrder(300m);
        order2.OrderDate = targetDate;

        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order1, order2 });

        var sut = new GetRevenueChartHandler(repo.Object);
        var result = await sut.Handle(
            new GetRevenueChartQuery(_tenantId, Days: 3), CancellationToken.None);

        result.Should().HaveCount(3);
        var point = result.FirstOrDefault(p => p.Date == targetDate);
        point.Should().NotBeNull();
        point!.Revenue.Should().Be(500m);
        point.OrderCount.Should().Be(2);
    }

    // ═══════════════════════════════════════════════════════════
    // GetInventoryStatisticsHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetInventoryStatistics_EmptyRepo_ReturnsZeros()
    {
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());
        productRepo.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var moveRepo = new Mock<IStockMovementRepository>();
        moveRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMovement>());

        var sut = new GetInventoryStatisticsHandler(productRepo.Object, moveRepo.Object);
        var result = await sut.Handle(
            new GetInventoryStatisticsQuery(), CancellationToken.None);

        result.TotalItems.Should().Be(0);
        result.TotalInventoryValue.Should().Be(0);
        result.OutOfStockCount.Should().Be(0);
    }

    [Fact]
    public async Task GetInventoryStatistics_WithProducts_CalculatesCorrectly()
    {
        var p1 = new Product { Id = Guid.NewGuid(), SalePrice = 100, MinimumStock = 5 };
        var p2 = new Product { Id = Guid.NewGuid(), SalePrice = 50, MinimumStock = 10 };
        p2.SyncStock(3);
        var p3 = new Product { Id = Guid.NewGuid(), SalePrice = 200, MinimumStock = 5 };
        p3.SyncStock(100);
        var products = new List<Product> { p1, p2, p3 };

        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);
        productRepo.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products.Where(p => p.Stock <= p.MinimumStock).ToList());

        var moveRepo = new Mock<IStockMovementRepository>();
        moveRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMovement>());

        var sut = new GetInventoryStatisticsHandler(productRepo.Object, moveRepo.Object);
        var result = await sut.Handle(
            new GetInventoryStatisticsQuery(), CancellationToken.None);

        result.TotalItems.Should().Be(3);
        result.OutOfStockCount.Should().Be(1);
        result.CriticalStockCount.Should().Be(1);
        result.TotalInventoryValue.Should().Be(0 * 100 + 3 * 50 + 100 * 200);
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════

    private static Order CreateOrder(decimal totalAmount)
    {
        var order = Order.CreateManual(
            Guid.NewGuid(), Guid.NewGuid(), "Test Customer", null, "SALE");
        order.AddItem(new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            Quantity = 1,
            UnitPrice = totalAmount,
            TotalPrice = totalAmount
        });
        return order;
    }
}
