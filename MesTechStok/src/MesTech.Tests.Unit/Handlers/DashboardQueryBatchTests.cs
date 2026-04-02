using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Dashboard.Queries.GetAppHubData;
using MesTech.Application.Features.Dashboard.Queries.GetLowStockAlerts;
using MesTech.Application.Features.Dashboard.Queries.GetPendingInvoices;
using MesTech.Application.Features.Dashboard.Queries.GetPlatformHealth;
using MesTech.Application.Features.Dashboard.Queries.GetRecentOrders;
using MesTech.Application.Features.Dashboard.Queries.GetRevenueChart;
using MesTech.Application.Features.Dashboard.Queries.GetSalesChartData;
using MesTech.Application.Features.Dashboard.Queries.GetSalesToday;
using MesTech.Application.Features.Dashboard.Queries.GetServiceHealth;
using MesTech.Application.Interfaces;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Application.Queries.GetProductDbStatus;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

// ════════════════════════════════════════════════════════
// DEV5 TUR 6: Dashboard handler batch tests — 9 handler
// Pattern: single-repo query handler → mock repo, verify call
// ════════════════════════════════════════════════════════

#region GetAppHubData

[Trait("Category", "Unit")]
[Trait("Layer", "Dashboard")]
public class GetAppHubDataHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallOrderRepoAndMediator()
    {
        var mediator = new Mock<ISender>();
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(42);
        mediator.Setup(m => m.Send(It.IsAny<GetProductDbStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDbStatusDto());
        mediator.Setup(m => m.Send(It.IsAny<GetInventoryStatisticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryStatisticsDto());
        mediator.Setup(m => m.Send(It.IsAny<GetPendingInvoicesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PendingInvoiceDto>().AsReadOnly());
        mediator.Setup(m => m.Send(It.IsAny<GetServiceHealthQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceHealthDto>().AsReadOnly());

        var sut = new GetAppHubDataHandler(mediator.Object, orderRepo.Object);
        var result = await sut.Handle(new GetAppHubDataQuery(Guid.NewGuid()), CancellationToken.None);

        orderRepo.Verify(r => r.GetCountAsync(It.IsAny<CancellationToken>()), Times.Once);
        result.TotalOrders.Should().Be(42);
    }
}

#endregion

#region GetLowStockAlerts

[Trait("Category", "Unit")]
[Trait("Layer", "Dashboard")]
public class GetLowStockAlertsHandlerTests2
{
    [Fact]
    public async Task Handle_ShouldCallProductRepo()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product>());
        var sut = new GetLowStockAlertsHandler(repo.Object);
        await sut.Handle(new GetLowStockAlertsQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetLowStockAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetPendingInvoices

[Trait("Category", "Unit")]
[Trait("Layer", "Dashboard")]
public class GetPendingInvoicesHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallInvoiceRepo()
    {
        var repo = new Mock<IInvoiceRepository>();
        repo.Setup(r => r.GetFailedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invoice>());
        var sut = new GetPendingInvoicesHandler(repo.Object);
        await sut.Handle(new GetPendingInvoicesQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetFailedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetPlatformHealth

[Trait("Category", "Unit")]
[Trait("Layer", "Dashboard")]
public class GetPlatformHealthHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallSyncLogRepo()
    {
        var repo = new Mock<MesTech.Application.Interfaces.ISyncLogRepository>();
        repo.Setup(r => r.GetLatestByPlatformAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncLog>());
        repo.Setup(r => r.GetFailedSinceAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncLog>());
        var sut = new GetPlatformHealthHandler(repo.Object);
        await sut.Handle(new GetPlatformHealthQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetLatestByPlatformAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetRecentOrders

[Trait("Category", "Unit")]
[Trait("Layer", "Dashboard")]
public class GetRecentOrdersHandlerTests2
{
    [Fact]
    public async Task Handle_ShouldCallOrderRepo()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());
        var sut = new GetRecentOrdersHandler(repo.Object);
        await sut.Handle(new GetRecentOrdersQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetRevenueChart

[Trait("Category", "Unit")]
[Trait("Layer", "Dashboard")]
public class GetRevenueChartHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallOrderRepo()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());
        var sut = new GetRevenueChartHandler(repo.Object);
        var result = await sut.Handle(new GetRevenueChartQuery(Guid.NewGuid(), 7), CancellationToken.None);
        repo.Verify(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        result.Should().NotBeNull();
    }
}

#endregion

#region GetSalesChartData

[Trait("Category", "Unit")]
[Trait("Layer", "Dashboard")]
public class GetSalesChartDataHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallOrderRepo()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());
        var sut = new GetSalesChartDataHandler(repo.Object);
        var result = await sut.Handle(new GetSalesChartDataQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        result.Should().NotBeNull();
    }
}

#endregion

#region GetSalesToday

[Trait("Category", "Unit")]
[Trait("Layer", "Dashboard")]
public class GetSalesTodayHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallOrderRepoTwice()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());
        var sut = new GetSalesTodayHandler(repo.Object);
        var result = await sut.Handle(new GetSalesTodayQuery(Guid.NewGuid()), CancellationToken.None);
        // Today + Yesterday = 2 calls
        repo.Verify(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        result.ChangePercent.Should().Be(0m);
    }
}

#endregion

#region GetServiceHealth

[Trait("Category", "Unit")]
[Trait("Layer", "Dashboard")]
public class GetServiceHealthHandlerTests2
{
    [Fact]
    public async Task Handle_ShouldCallHealthService()
    {
        var healthService = new Mock<IInfrastructureHealthService>();
        healthService.Setup(h => h.CheckAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceHealthResult>
            {
                new("PostgreSQL", true, "5ms")
            });
        var sut = new GetServiceHealthHandler(healthService.Object);
        var result = await sut.Handle(new GetServiceHealthQuery(), CancellationToken.None);
        healthService.Verify(h => h.CheckAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        result.Should().HaveCount(1);
    }
}

#endregion
