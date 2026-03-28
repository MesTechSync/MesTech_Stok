using FluentAssertions;
using MesTech.Application.Features.Product.Queries.GetProducts;
using MesTech.Application.Features.Stock.Queries.GetStockSummary;
using MesTech.Application.Features.Stock.Queries.GetStockTransfers;
using MesTech.Application.Features.Dashboard.Queries.GetStockAlerts;
using MesTech.Application.Features.Dashboard.Queries.GetLowStockAlerts;
using MesTech.Application.Features.Dashboard.Queries.GetRecentOrders;
using MesTech.Application.Features.Dashboard.Queries.GetOrdersPending;
using MesTech.Application.Features.Dashboard.Queries.GetTopProducts;
using MesTech.Application.Features.Orders.Queries.GetOrderList;
using MesTech.Application.Features.Orders.Queries.GetOrdersByStatus;
using MesTech.Application.Features.Reports.InventoryValuationReport;
using MesTech.Application.Features.Reports.StockTurnoverReport;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipProducts;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipOrders;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Common;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// Stock/Inventory Query Handler testleri.
/// Null-guard + empty data + happy path.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Stock")]
[Trait("Group", "QueryHandler")]
public class StockQueryHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══ GetProducts ═══

    [Fact]
    public async Task GetProducts_NullRequest_Throws()
    {
        var repo = new Mock<IProductRepository>();
        var handler = new GetProductsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetProducts_EmptyRepo_ReturnsEmptyPage()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Product>());

        var handler = new GetProductsHandler(repo.Object);
        var result = await handler.Handle(
            new GetProductsQuery(_tenantId), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetProducts_WithSearchTerm_PassesFilterToRepo()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.SearchAsync("iPhone"))
            .ReturnsAsync(new List<Product>());

        var handler = new GetProductsHandler(repo.Object);
        await handler.Handle(
            new GetProductsQuery(_tenantId, SearchTerm: "iPhone"), CancellationToken.None);

        repo.Verify(r => r.SearchAsync("iPhone"), Times.Once);
    }

    // ═══ GetStockSummary ═══

    [Fact]
    public async Task GetStockSummary_NullRequest_Throws()
    {
        var repo = new Mock<IProductRepository>();
        var handler = new GetStockSummaryHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetStockSummary_EmptyRepo_ReturnsZeroTotals()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Product>());

        var handler = new GetStockSummaryHandler(repo.Object);
        var result = await handler.Handle(
            new GetStockSummaryQuery(_tenantId), CancellationToken.None);

        result.TotalProducts.Should().Be(0);
        result.TotalUnits.Should().Be(0);
        result.TotalStockValue.Should().Be(0);
    }

    // ═══ GetStockTransfers ═══

    [Fact]
    public async Task GetStockTransfers_NullRequest_Throws()
    {
        var repo = new Mock<IStockMovementRepository>();
        var handler = new GetStockTransfersHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetStockTransfers_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IStockMovementRepository>();
        repo.Setup(r => r.GetRecentAsync(_tenantId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMovement>());

        var handler = new GetStockTransfersHandler(repo.Object);
        var result = await handler.Handle(
            new GetStockTransfersQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetStockAlerts ═══

    [Fact]
    public async Task GetStockAlerts_NullRequest_Throws()
    {
        var repo = new Mock<IProductRepository>();
        var handler = new GetStockAlertsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetStockAlerts_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetLowStockAsync())
            .ReturnsAsync(new List<Product>());

        var handler = new GetStockAlertsHandler(repo.Object);
        var result = await handler.Handle(
            new GetStockAlertsQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetLowStockAlerts ═══

    [Fact]
    public async Task GetLowStockAlerts_NullRequest_Throws()
    {
        var repo = new Mock<IProductRepository>();
        var handler = new GetLowStockAlertsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetLowStockAlerts_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetLowStockAsync())
            .ReturnsAsync(new List<Product>());

        var handler = new GetLowStockAlertsHandler(repo.Object);
        var result = await handler.Handle(
            new GetLowStockAlertsQuery(_tenantId, 20), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetRecentOrders ═══

    [Fact]
    public async Task GetRecentOrders_NullRequest_Throws()
    {
        var repo = new Mock<IOrderRepository>();
        var handler = new GetRecentOrdersHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetRecentOrders_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var handler = new GetRecentOrdersHandler(repo.Object);
        var result = await handler.Handle(
            new GetRecentOrdersQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetOrdersPending ═══

    [Fact]
    public async Task GetOrdersPending_NullRequest_Throws()
    {
        var repo = new Mock<IOrderRepository>();
        var handler = new GetOrdersPendingHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetOrdersPending_EmptyRepo_ReturnsZeroCount()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var handler = new GetOrdersPendingHandler(repo.Object);
        var result = await handler.Handle(
            new GetOrdersPendingQuery(_tenantId), CancellationToken.None);

        result.Count.Should().Be(0);
    }

    // ═══ GetTopProducts ═══

    [Fact]
    public async Task GetTopProducts_NullRequest_Throws()
    {
        var repo = new Mock<IOrderRepository>();
        var handler = new GetTopProductsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetOrderList ═══

    [Fact]
    public async Task GetOrderList_NullRequest_Throws()
    {
        var repo = new Mock<IOrderRepository>();
        var handler = new GetOrderListHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetOrderList_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetRecentAsync(_tenantId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var handler = new GetOrderListHandler(repo.Object);
        var result = await handler.Handle(
            new GetOrderListQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetOrdersByStatus ═══

    [Fact]
    public async Task GetOrdersByStatus_NullRequest_Throws()
    {
        var repo = new Mock<IOrderRepository>();
        var handler = new GetOrdersByStatusHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ InventoryValuationReport ═══

    [Fact]
    public async Task InventoryValuationReport_NullRequest_Throws()
    {
        var repo = new Mock<IProductRepository>();
        var handler = new InventoryValuationReportHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task InventoryValuationReport_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Product>());

        var handler = new InventoryValuationReportHandler(repo.Object);
        var result = await handler.Handle(
            new InventoryValuationReportQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ StockTurnoverReport ═══

    [Fact]
    public async Task StockTurnoverReport_NullRequest_Throws()
    {
        var movementRepo = new Mock<IStockMovementRepository>();
        var productRepo = new Mock<IProductRepository>();
        var handler = new StockTurnoverReportHandler(movementRepo.Object, productRepo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetDropshipProducts ═══

    [Fact]
    public async Task GetDropshipProducts_NullRequest_Throws()
    {
        var repo = new Mock<IDropshipProductRepository>();
        var handler = new GetDropshipProductsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetDropshipProducts_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IDropshipProductRepository>();
        repo.Setup(r => r.GetByTenantAsync(_tenantId, It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Dropshipping.Entities.DropshipProduct>());

        var handler = new GetDropshipProductsHandler(repo.Object);
        var result = await handler.Handle(
            new GetDropshipProductsQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetDropshipOrders ═══

    [Fact]
    public async Task GetDropshipOrders_NullRequest_Throws()
    {
        var repo = new Mock<IDropshipOrderRepository>();
        var handler = new GetDropshipOrdersHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetDropshipOrders_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IDropshipOrderRepository>();
        repo.Setup(r => r.GetByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Dropshipping.Entities.DropshipOrder>());

        var handler = new GetDropshipOrdersHandler(repo.Object);
        var result = await handler.Handle(
            new GetDropshipOrdersQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
