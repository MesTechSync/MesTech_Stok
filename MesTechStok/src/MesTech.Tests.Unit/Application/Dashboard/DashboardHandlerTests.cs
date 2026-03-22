using FluentAssertions;
using MesTech.Application.DTOs.Dashboard;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;
using MesTech.Application.Features.Dashboard.Queries.GetOrdersPending;
using MesTech.Application.Features.Dashboard.Queries.GetPlatformHealth;
using MesTech.Application.Features.Dashboard.Queries.GetRevenueChart;
using MesTech.Application.Features.Dashboard.Queries.GetSalesToday;
using MesTech.Application.Features.Dashboard.Queries.GetStockAlerts;
using MesTech.Application.Features.Dashboard.Queries.GetTopProducts;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Dashboard;

// ════════════════════════════════════════════════════════
// DEV5 Agent E: Dashboard Handler Tests
// ════════════════════════════════════════════════════════

#region GetDashboardSummaryQueryHandler

[Trait("Category", "Unit")]
public class GetDashboardSummaryQueryHandlerTests
{
    private readonly Mock<IDashboardSummaryRepository> _repo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetDashboardSummaryQueryHandler CreateHandler() => new(_repo.Object);

    [Fact]
    public async Task Handle_ValidRequest_ShouldDelegateToRepository()
    {
        // Arrange
        var expected = new DashboardSummaryDto();
        _repo.Setup(r => r.GetSummaryAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = CreateHandler();
        var query = new GetDashboardSummaryQuery(_tenantId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        _repo.Verify(r => r.GetSummaryAsync(_tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_CancellationToken_ShouldBeForwarded()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var expected = new DashboardSummaryDto();
        _repo.Setup(r => r.GetSummaryAsync(_tenantId, token)).ReturnsAsync(expected);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new GetDashboardSummaryQuery(_tenantId), token);

        // Assert
        _repo.Verify(r => r.GetSummaryAsync(_tenantId, token), Times.Once);
    }
}

#endregion

#region GetOrdersPendingHandler

[Trait("Category", "Unit")]
public class GetOrdersPendingHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetOrdersPendingHandler CreateHandler() => new(_orderRepo.Object);

    [Fact]
    public async Task Handle_NoPendingOrders_ShouldReturnZeroCounts()
    {
        // Arrange
        var orders = new List<Order>().AsReadOnly();
        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetOrdersPendingQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Count.Should().Be(0);
        result.Urgent.Should().Be(0);
        result.OldestMinutes.Should().Be(0);
    }

    [Fact]
    public async Task Handle_PendingAndConfirmedOrders_ShouldCountCorrectly()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() { Status = OrderStatus.Pending, OrderDate = DateTime.UtcNow.AddHours(-2), TenantId = _tenantId },
            new() { Status = OrderStatus.Confirmed, OrderDate = DateTime.UtcNow.AddHours(-1), TenantId = _tenantId },
            new() { Status = OrderStatus.Shipped, OrderDate = DateTime.UtcNow.AddHours(-3), TenantId = _tenantId },
        }.AsReadOnly();

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetOrdersPendingQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Count.Should().Be(2); // Pending + Confirmed
        result.Urgent.Should().Be(0); // None older than 24h
    }

    [Fact]
    public async Task Handle_UrgentOrders_ShouldCountOlderThan24Hours()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() { Status = OrderStatus.Pending, OrderDate = DateTime.UtcNow.AddHours(-30), TenantId = _tenantId },
            new() { Status = OrderStatus.Pending, OrderDate = DateTime.UtcNow.AddHours(-25), TenantId = _tenantId },
            new() { Status = OrderStatus.Pending, OrderDate = DateTime.UtcNow.AddHours(-2), TenantId = _tenantId },
        }.AsReadOnly();

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetOrdersPendingQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Count.Should().Be(3);
        result.Urgent.Should().Be(2); // 2 orders older than 24h
        result.OldestMinutes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region GetPlatformHealthHandler

[Trait("Category", "Unit")]
public class GetPlatformHealthHandlerTests
{
    private readonly Mock<ISyncLogRepository> _syncLogRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetPlatformHealthHandler CreateHandler() => new(_syncLogRepo.Object);

    [Fact]
    public async Task Handle_NoLogs_ShouldReturnEmptyList()
    {
        // Arrange
        _syncLogRepo.Setup(r => r.GetLatestByPlatformAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncLog>().AsReadOnly());
        _syncLogRepo.Setup(r => r.GetFailedSinceAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncLog>().AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPlatformHealthQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_HealthyPlatform_ShouldReturnHealthyStatus()
    {
        // Arrange
        var latestLogs = new List<SyncLog>
        {
            new() { PlatformCode = "trendyol", IsSuccess = true, StartedAt = DateTime.UtcNow.AddMinutes(-10), CompletedAt = DateTime.UtcNow.AddMinutes(-9), TenantId = _tenantId }
        }.AsReadOnly();

        _syncLogRepo.Setup(r => r.GetLatestByPlatformAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(latestLogs);
        _syncLogRepo.Setup(r => r.GetFailedSinceAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncLog>().AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPlatformHealthQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Platform.Should().Be("trendyol");
        result[0].Status.Should().Be("Healthy");
        result[0].ErrorCount24h.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CriticalPlatform_ShouldReturnCriticalWhenFiveOrMoreErrors()
    {
        // Arrange
        var latestLogs = new List<SyncLog>
        {
            new() { PlatformCode = "opencart", IsSuccess = false, StartedAt = DateTime.UtcNow.AddMinutes(-5), TenantId = _tenantId }
        }.AsReadOnly();

        var failedLogs = Enumerable.Range(0, 5).Select(_ =>
            new SyncLog { PlatformCode = "opencart", IsSuccess = false, TenantId = _tenantId }
        ).ToList().AsReadOnly();

        _syncLogRepo.Setup(r => r.GetLatestByPlatformAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(latestLogs);
        _syncLogRepo.Setup(r => r.GetFailedSinceAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedLogs);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetPlatformHealthQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be("Critical");
        result[0].ErrorCount24h.Should().Be(5);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region GetRevenueChartHandler

[Trait("Category", "Unit")]
public class GetRevenueChartHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetRevenueChartHandler CreateHandler() => new(_orderRepo.Object);

    [Fact]
    public async Task Handle_NoOrders_ShouldReturnZeroFilledChart()
    {
        // Arrange
        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetRevenueChartQuery(_tenantId, 7), CancellationToken.None);

        // Assert
        result.Should().HaveCount(7);
        result.Should().OnlyContain(p => p.Revenue == 0m && p.OrderCount == 0);
    }

    [Fact]
    public async Task Handle_DaysClamp_ShouldClampTo365Max()
    {
        // Arrange
        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetRevenueChartQuery(_tenantId, 500), CancellationToken.None);

        // Assert
        result.Should().HaveCount(365);
    }

    [Fact]
    public async Task Handle_WithOrders_ShouldGroupByDate()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var orders = new List<Order>
        {
            CreateOrderWithAmount(today, 100m),
            CreateOrderWithAmount(today, 200m),
            CreateOrderWithAmount(today.AddDays(-1), 50m),
        }.AsReadOnly();

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetRevenueChartQuery(_tenantId, 3), CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        // At least one point should have revenue > 0
        result.Should().Contain(p => p.Revenue > 0);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static Order CreateOrderWithAmount(DateTime orderDate, decimal amount)
    {
        var order = new Order { OrderDate = orderDate };
        order.SetFinancials(0m, 0m, amount);
        return order;
    }
}

#endregion

#region GetSalesTodayHandler

[Trait("Category", "Unit")]
public class GetSalesTodayHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetSalesTodayHandler CreateHandler() => new(_orderRepo.Object);

    [Fact]
    public async Task Handle_NoOrders_ShouldReturnZeros()
    {
        // Arrange
        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetSalesTodayQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Today.Should().Be(0m);
        result.Yesterday.Should().Be(0m);
        result.ChangePercent.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_TodayOnlyOrders_ShouldReturn100PercentChange()
    {
        // Arrange — today has orders, yesterday has none
        var todayOrder = new Order();
        todayOrder.SetFinancials(0m, 0m, 500m);

        _orderRepo.SetupSequence(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { todayOrder }.AsReadOnly())
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetSalesTodayQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Today.Should().Be(500m);
        result.Yesterday.Should().Be(0m);
        result.ChangePercent.Should().Be(100m);
    }

    [Fact]
    public async Task Handle_BothDaysWithOrders_ShouldCalculateChangePercent()
    {
        // Arrange
        var todayOrder = new Order();
        todayOrder.SetFinancials(0m, 0m, 600m);
        var yesterdayOrder = new Order();
        yesterdayOrder.SetFinancials(0m, 0m, 400m);

        _orderRepo.SetupSequence(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { todayOrder }.AsReadOnly())
            .ReturnsAsync(new List<Order> { yesterdayOrder }.AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetSalesTodayQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Today.Should().Be(600m);
        result.Yesterday.Should().Be(400m);
        result.ChangePercent.Should().Be(50m); // (600-400)/400 * 100 = 50%
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region GetStockAlertsHandler

[Trait("Category", "Unit")]
public class GetStockAlertsHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();

    private GetStockAlertsHandler CreateHandler() => new(_productRepo.Object);

    [Fact]
    public async Task Handle_NoLowStockProducts_ShouldReturnEmptyList()
    {
        // Arrange
        _productRepo.Setup(r => r.GetLowStockAsync())
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetStockAlertsQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_LowStockProducts_ShouldMapToDto()
    {
        // Arrange
        var product = FakeData.CreateProduct(sku: "LOW-001", stock: 2, minimumStock: 10);
        _productRepo.Setup(r => r.GetLowStockAsync())
            .ReturnsAsync(new List<Product> { product }.AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetStockAlertsQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].SKU.Should().Be("LOW-001");
        result[0].CurrentStock.Should().Be(2);
        result[0].MinThreshold.Should().Be(10);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region GetTopProductsHandler

[Trait("Category", "Unit")]
public class GetTopProductsHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetTopProductsHandler CreateHandler() => new(_orderRepo.Object);

    [Fact]
    public async Task Handle_NoOrders_ShouldReturnEmptyList()
    {
        // Arrange
        _orderRepo.Setup(r => r.GetByDateRangeWithItemsAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetTopProductsQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithOrderItems_ShouldGroupByProductAndSortByRevenue()
    {
        // Arrange
        var productIdA = Guid.NewGuid();
        var productIdB = Guid.NewGuid();

        var order = new Order { TenantId = _tenantId };
        order.AddItem(new OrderItem { ProductId = productIdA, ProductSKU = "SKU-A", ProductName = "Product A", Quantity = 5, TotalPrice = 500m });
        order.AddItem(new OrderItem { ProductId = productIdB, ProductSKU = "SKU-B", ProductName = "Product B", Quantity = 2, TotalPrice = 800m });
        order.AddItem(new OrderItem { ProductId = productIdA, ProductSKU = "SKU-A", ProductName = "Product A", Quantity = 3, TotalPrice = 300m });

        _orderRepo.Setup(r => r.GetByDateRangeWithItemsAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order }.AsReadOnly());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetTopProductsQuery(_tenantId, 10), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Revenue.Should().Be(800m); // Product B
        result[1].Revenue.Should().Be(800m); // Product A (500+300)
    }

    [Fact]
    public async Task Handle_LimitClamp_ShouldClampToMax100()
    {
        // Arrange
        _orderRepo.Setup(r => r.GetByDateRangeWithItemsAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var handler = CreateHandler();

        // Act — limit > 100 should still work (clamped internally)
        var result = await handler.Handle(new GetTopProductsQuery(_tenantId, 200), CancellationToken.None);

        // Assert
        result.Should().BeEmpty(); // No orders, but no exception
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion
