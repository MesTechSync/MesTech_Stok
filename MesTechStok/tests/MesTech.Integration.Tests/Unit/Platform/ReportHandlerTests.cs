using FluentAssertions;
using MesTech.Application.Features.Reports.CargoPerformanceReport;
using MesTech.Application.Features.Reports.CommissionReport;
using MesTech.Application.Features.Reports.CustomerLifetimeValueReport;
using MesTech.Application.Features.Reports.CustomerSegmentReport;
using MesTech.Application.Features.Reports.FulfillmentCostReport;
using MesTech.Application.Features.Reports.OrderFulfillmentReport;
using MesTech.Application.Features.Reports.PlatformPerformanceReport;
using MesTech.Application.Features.Reports.PlatformSalesReport;
using MesTech.Application.Features.Reports.ProfitabilityReport;
using MesTech.Application.Features.Reports.StockTurnoverReport;
using MesTech.Application.Features.Reports.TaxSummaryReport;
using MesTech.Application.Features.Reports.SalesAnalytics;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

[Trait("Category", "Unit")]
[Trait("Layer", "Reports")]
[Trait("Group", "Handler")]
public class ReportHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly DateTime _from = new(2026, 1, 1);
    private readonly DateTime _to = new(2026, 3, 31);

    // ═══ CargoPerformanceReport ═══

    [Fact]
    public async Task CargoPerformanceReport_NullRequest_Throws()
    {
        var cargoRepo = new Mock<ICargoExpenseRepository>();
        var orderRepo = new Mock<IOrderRepository>();
        var handler = new CargoPerformanceReportHandler(cargoRepo.Object, orderRepo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CargoPerformanceReport_EmptyData_ReturnsEmptyList()
    {
        var cargoRepo = new Mock<ICargoExpenseRepository>();
        var orderRepo = new Mock<IOrderRepository>();
        cargoRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, _from, _to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.CargoExpense>());
        orderRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, _from, _to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Order>());

        var handler = new CargoPerformanceReportHandler(cargoRepo.Object, orderRepo.Object);
        var result = await handler.Handle(new CargoPerformanceReportQuery(_tenantId, _from, _to), CancellationToken.None);
        result.Should().BeEmpty();
    }

    // ═══ CommissionReport ═══

    [Fact]
    public async Task CommissionReport_NullRequest_Throws()
    {
        var repo = new Mock<ICommissionRecordRepository>();
        var handler = new CommissionReportHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CustomerLifetimeValue ═══

    [Fact]
    public async Task CustomerLifetimeValue_NullRequest_Throws()
    {
        var repo = new Mock<IOrderRepository>();
        var handler = new CustomerLifetimeValueReportHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CustomerSegment ═══

    [Fact]
    public async Task CustomerSegment_NullRequest_Throws()
    {
        var repo = new Mock<IOrderRepository>();
        var handler = new CustomerSegmentReportHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ FulfillmentCost ═══

    [Fact]
    public async Task FulfillmentCost_NullRequest_Throws()
    {
        var factory = new Mock<IFulfillmentProviderFactory>();
        var logger = Mock.Of<ILogger<FulfillmentCostReportHandler>>();
        var handler = new FulfillmentCostReportHandler(factory.Object, logger);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ OrderFulfillment ═══

    [Fact]
    public async Task OrderFulfillment_NullRequest_Throws()
    {
        var repo = new Mock<IOrderRepository>();
        var handler = new OrderFulfillmentReportHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ PlatformPerformance ═══

    [Fact]
    public async Task PlatformPerformance_NullRequest_Throws()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var commRepo = new Mock<ICommissionRecordRepository>();
        var handler = new PlatformPerformanceReportHandler(orderRepo.Object, commRepo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ PlatformSales ═══

    [Fact]
    public async Task PlatformSales_NullRequest_Throws()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var commRepo = new Mock<ICommissionRecordRepository>();
        var settleRepo = new Mock<ISettlementBatchRepository>();
        var handler = new PlatformSalesReportHandler(orderRepo.Object, commRepo.Object, settleRepo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ Profitability ═══

    [Fact]
    public async Task Profitability_NullRequest_Throws()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var productRepo = new Mock<IProductRepository>();
        var logger = Mock.Of<ILogger<ProfitabilityReportHandler>>();
        var handler = new ProfitabilityReportHandler(orderRepo.Object, productRepo.Object, logger);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ StockTurnover ═══

    [Fact]
    public async Task StockTurnover_NullRequest_Throws()
    {
        var movementRepo = new Mock<IStockMovementRepository>();
        var productRepo = new Mock<IProductRepository>();
        var handler = new StockTurnoverReportHandler(movementRepo.Object, productRepo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ TaxSummary ═══

    [Fact]
    public async Task TaxSummary_NullRequest_Throws()
    {
        var repo = new Mock<IOrderRepository>();
        var handler = new TaxSummaryReportHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ SalesAnalytics ═══

    [Fact]
    public async Task SalesAnalytics_NullRequest_Throws()
    {
        var repo = new Mock<IOrderRepository>();
        var handler = new GetSalesAnalyticsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }
}
