using FluentAssertions;
using MesTech.Application.Features.Reports.PlatformPerformanceReport;
using MesTech.Application.Features.Reports.PlatformSalesReport;
using MesTech.Application.Features.Reports.ProfitabilityReport;
using MesTech.Application.Features.Reports.SalesAnalytics;
using MesTech.Application.Features.Reports.StockTurnoverReport;
using MesTech.Application.Features.Reports.TaxSummaryReport;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

// ════════════════════════════════════════════════════════
// DEV5: Reports handler batch tests — 6 handlers
// Pattern: mock repo → verify call or assert result
// ════════════════════════════════════════════════════════

#region PlatformPerformanceReport

[Trait("Category", "Unit")]
[Trait("Layer", "Reports")]
public class PlatformPerformanceReportHandlerTests2
{
    [Fact]
    public async Task Handle_EmptyOrders_ShouldReturnEmptyPlatforms()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var commissionRepo = new Mock<ICommissionRecordRepository>();

        orderRepo.Setup(r => r.GetByDateRangeWithItemsAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var sut = new PlatformPerformanceReportHandler(orderRepo.Object, commissionRepo.Object);
        var query = new PlatformPerformanceReportQuery(Guid.NewGuid(), DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Platforms.Should().BeEmpty();
        orderRepo.Verify(r => r.GetByDateRangeWithItemsAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region PlatformSalesReport

[Trait("Category", "Unit")]
[Trait("Layer", "Reports")]
public class PlatformSalesReportHandlerTests
{
    [Fact]
    public async Task Handle_EmptyOrders_ShouldReturnEmptyList()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var commissionRepo = new Mock<ICommissionRecordRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();

        orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());
        settlementRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch>());

        var sut = new PlatformSalesReportHandler(orderRepo.Object, commissionRepo.Object, settlementRepo.Object);
        var query = new PlatformSalesReportQuery(Guid.NewGuid(), DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}

#endregion

#region ProfitabilityReport

[Trait("Category", "Unit")]
[Trait("Layer", "Reports")]
public class ProfitabilityReportHandlerTests
{
    [Fact]
    public async Task Handle_EmptyOrders_ShouldReturnEmptyDto()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var productRepo = new Mock<IProductRepository>();
        var logger = new Mock<ILogger<ProfitabilityReportHandler>>();

        orderRepo.Setup(r => r.GetByDateRangeWithItemsAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var sut = new ProfitabilityReportHandler(orderRepo.Object, productRepo.Object, logger.Object);
        var query = new ProfitabilityReportQuery(Guid.NewGuid(), DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalOrders.Should().Be(0);
        result.NetProfit.Should().Be(0);
    }
}

#endregion

#region SalesAnalytics

[Trait("Category", "Unit")]
[Trait("Layer", "Reports")]
public class GetSalesAnalyticsHandlerTests
{
    [Fact]
    public async Task Handle_EmptyOrders_ShouldReturnDefaultDto()
    {
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByDateRangeWithItemsAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var sut = new GetSalesAnalyticsHandler(orderRepo.Object);
        var query = new GetSalesAnalyticsQuery(Guid.NewGuid(), DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalOrders.Should().Be(0);
        result.TotalRevenue.Should().Be(0);
        orderRepo.Verify(r => r.GetByDateRangeWithItemsAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region StockTurnoverReport

[Trait("Category", "Unit")]
[Trait("Layer", "Reports")]
public class StockTurnoverReportHandlerTests
{
    [Fact]
    public async Task Handle_EmptyMovements_ShouldReturnEmptyList()
    {
        var movementRepo = new Mock<IStockMovementRepository>();
        var productRepo = new Mock<IProductRepository>();

        movementRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMovement>());
        productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var sut = new StockTurnoverReportHandler(movementRepo.Object, productRepo.Object);
        var query = new StockTurnoverReportQuery(Guid.NewGuid(), DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
        movementRepo.Verify(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region TaxSummaryReport

[Trait("Category", "Unit")]
[Trait("Layer", "Reports")]
public class TaxSummaryReportHandlerTests2
{
    [Fact]
    public async Task Handle_EmptyOrders_ShouldReturnEmptyList()
    {
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var sut = new TaxSummaryReportHandler(orderRepo.Object);
        var query = new TaxSummaryReportQuery(Guid.NewGuid(), DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
        orderRepo.Verify(r => r.GetByDateRangeAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion
