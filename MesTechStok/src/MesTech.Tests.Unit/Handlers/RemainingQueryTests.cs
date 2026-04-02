using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.DTOs.Shipping;
using MesTech.Application.DTOs.Tasks;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenses;
using MesTech.Application.Features.Accounting.Queries.GetPendingReviews;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationDashboard;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationMatches;
using MesTech.Application.Features.Accounting.Queries.GetShipmentCosts;
using MesTech.Application.Features.Accounting.Queries.ListFixedAssets;
using MesTech.Application.Features.Accounting.Queries.ListTaxWithholdings;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentInventory;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentOrders;
using MesTech.Application.Features.Onboarding.Queries.GetV5ReadinessCheck;
using MesTech.Application.Features.Reports.InventoryValuationReport;
using MesTech.Application.Features.Reports.OrderFulfillmentReport;
using MesTech.Application.Features.Reports.PlatformPerformanceReport;
using MesTech.Application.Features.Reports.PlatformSalesReport;
using MesTech.Application.Features.Reports.ProfitabilityReport;
using MesTech.Application.Features.Reports.StockTurnoverReport;
using MesTech.Application.Features.Shipping.Queries.GetShipmentStatus;
using MesTech.Application.Features.Tasks.Queries.GetProjects;
using MesTech.Application.Features.Tasks.Queries.GetProjectTasks;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Erp;
using MesTech.Application.Queries.GetQuotationById;
using MesTech.Application.Queries.GetSyncStatus;
using MesTech.Application.Queries.ListQuotations;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for remaining List/Get query handlers and report handlers.
/// Pattern: Moq repos return empty lists, verify handler returns non-null result.
/// </summary>
[Trait("Category", "Unit")]
public class RemainingQueryTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    #region ListFixedAssetsHandler

    [Fact]
    public async Task ListFixedAssetsHandler_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IFixedAssetRepository>();
        repo.Setup(r => r.GetAllAsync(TenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.FixedAsset>().AsReadOnly());

        var sut = new ListFixedAssetsHandler(repo.Object);
        var result = await sut.Handle(new ListFixedAssetsQuery(TenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListFixedAssetsHandler_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IFixedAssetRepository>();
        var sut = new ListFixedAssetsHandler(repo.Object);

        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetFixedExpensesHandler

    [Fact]
    public async Task GetFixedExpensesHandler_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        repo.Setup(r => r.GetAllAsync(TenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.FixedExpense>().AsReadOnly());

        var sut = new GetFixedExpensesHandler(repo.Object);
        var result = await sut.Handle(new GetFixedExpensesQuery(TenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFixedExpensesHandler_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var sut = new GetFixedExpensesHandler(repo.Object);

        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region ListTaxWithholdingsHandler

    [Fact]
    public async Task ListTaxWithholdingsHandler_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<ITaxWithholdingRepository>();
        repo.Setup(r => r.GetAllAsync(TenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.TaxWithholding>().AsReadOnly());

        var sut = new ListTaxWithholdingsHandler(repo.Object);
        var result = await sut.Handle(new ListTaxWithholdingsQuery(TenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListTaxWithholdingsHandler_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ITaxWithholdingRepository>();
        var sut = new ListTaxWithholdingsHandler(repo.Object);

        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetReconciliationMatchesHandler

    [Fact]
    public async Task GetReconciliationMatchesHandler_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IReconciliationMatchRepository>();
        repo.Setup(r => r.GetByStatusAsync(TenantId, ReconciliationStatus.NeedsReview, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch>().AsReadOnly());

        var sut = new GetReconciliationMatchesHandler(repo.Object);
        var result = await sut.Handle(
            new GetReconciliationMatchesQuery(TenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetReconciliationMatchesHandler_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IReconciliationMatchRepository>();
        var sut = new GetReconciliationMatchesHandler(repo.Object);

        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetReconciliationDashboardHandler

    [Fact]
    public async Task GetReconciliationDashboardHandler_EmptyRepo_ReturnsZeroCounts()
    {
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();

        matchRepo.Setup(r => r.GetByStatusAsync(TenantId, It.IsAny<ReconciliationStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch>().AsReadOnly());
        settlementRepo.Setup(r => r.GetUnmatchedAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.SettlementBatch>().AsReadOnly());

        var sut = new GetReconciliationDashboardHandler(matchRepo.Object, settlementRepo.Object);
        var result = await sut.Handle(
            new GetReconciliationDashboardQuery(TenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.AutoMatchedCount.Should().Be(0);
        result.NeedsReviewCount.Should().Be(0);
        result.UnmatchedCount.Should().Be(0);
    }

    [Fact]
    public async Task GetReconciliationDashboardHandler_NullRequest_ThrowsArgumentNullException()
    {
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var sut = new GetReconciliationDashboardHandler(matchRepo.Object, settlementRepo.Object);

        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetPendingReviewsHandler

    [Fact]
    public async Task GetPendingReviewsHandler_EmptyRepo_ReturnsEmptyResult()
    {
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var bankTxRepo = new Mock<IBankTransactionRepository>();

        matchRepo.Setup(r => r.GetPendingReviewsPagedAsync(
                TenantId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ReconciliationMatch>().AsReadOnly(), 0));

        var sut = new GetPendingReviewsHandler(matchRepo.Object, settlementRepo.Object, bankTxRepo.Object);
        var result = await sut.Handle(
            new GetPendingReviewsQuery(TenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetPendingReviewsHandler_NullRequest_ThrowsArgumentNullException()
    {
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var bankTxRepo = new Mock<IBankTransactionRepository>();
        var sut = new GetPendingReviewsHandler(matchRepo.Object, settlementRepo.Object, bankTxRepo.Object);

        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetShipmentCostsHandler

    [Fact]
    public async Task GetShipmentCostsHandler_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IShipmentCostRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.ShipmentCost>().AsReadOnly());

        var sut = new GetShipmentCostsHandler(repo.Object, NullLogger<GetShipmentCostsHandler>.Instance);
        var result = await sut.Handle(
            new GetShipmentCostsQuery(TenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetProjectsHandler

    [Fact]
    public async Task GetProjectsHandler_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IProjectRepository>();
        repo.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>().AsReadOnly());

        var sut = new GetProjectsHandler(repo.Object);
        var result = await sut.Handle(new GetProjectsQuery(TenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectsHandler_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IProjectRepository>();
        var sut = new GetProjectsHandler(repo.Object);

        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetProjectTasksHandler

    [Fact]
    public async Task GetProjectTasksHandler_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IWorkTaskRepository>();
        var projectId = Guid.NewGuid();
        repo.Setup(r => r.GetByProjectAsync(projectId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkTask>().AsReadOnly());

        var sut = new GetProjectTasksHandler(repo.Object);
        var result = await sut.Handle(
            new GetProjectTasksQuery(projectId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectTasksHandler_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IWorkTaskRepository>();
        var sut = new GetProjectTasksHandler(repo.Object);

        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetQuotationByIdHandler

    [Fact]
    public async Task GetQuotationByIdHandler_NotFound_ReturnsNull()
    {
        var repo = new Mock<IQuotationRepository>();
        repo.Setup(r => r.GetByIdWithLinesAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Quotation?)null);

        var sut = new GetQuotationByIdHandler(repo.Object);
        var result = await sut.Handle(
            new GetQuotationByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public void GetQuotationByIdHandler_NullRepo_ThrowsArgumentNullException()
    {
        var act = () => new GetQuotationByIdHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ListQuotationsHandler

    [Fact]
    public async Task ListQuotationsHandler_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IQuotationRepository>();
        repo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Quotation>().AsReadOnly());

        var sut = new ListQuotationsHandler(repo.Object);
        var result = await sut.Handle(
            new ListQuotationsQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListQuotationsHandler_WithStatusFilter_CallsGetByStatusAsync()
    {
        var repo = new Mock<IQuotationRepository>();
        repo.Setup(r => r.GetByStatusAsync(QuotationStatus.Draft))
            .ReturnsAsync(new List<Quotation>().AsReadOnly());

        var sut = new ListQuotationsHandler(repo.Object);
        var result = await sut.Handle(
            new ListQuotationsQuery(QuotationStatus.Draft), CancellationToken.None);

        result.Should().NotBeNull();
        repo.Verify(r => r.GetByStatusAsync(QuotationStatus.Draft), Times.Once);
    }

    #endregion

    #region GetSyncStatusHandler

    [Fact]
    public async Task GetSyncStatusHandler_NoAdapters_ReturnsEmptyPlatforms()
    {
        var orchestrator = new Mock<IIntegratorOrchestrator>();
        orchestrator.Setup(o => o.RegisteredAdapters)
            .Returns(new List<IIntegratorAdapter>().AsReadOnly());

        var sut = new GetSyncStatusHandler(orchestrator.Object);
        var result = await sut.Handle(
            new GetSyncStatusQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Platforms.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSyncStatusHandler_WithPlatformFilter_FiltersResults()
    {
        var adapter = new Mock<IIntegratorAdapter>();
        adapter.Setup(a => a.PlatformCode).Returns("Trendyol");

        var orchestrator = new Mock<IIntegratorOrchestrator>();
        orchestrator.Setup(o => o.RegisteredAdapters)
            .Returns(new List<IIntegratorAdapter> { adapter.Object }.AsReadOnly());

        var sut = new GetSyncStatusHandler(orchestrator.Object);
        var result = await sut.Handle(
            new GetSyncStatusQuery("OpenCart"), CancellationToken.None);

        result.Platforms.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSyncStatusHandler_NullRequest_ThrowsArgumentNullException()
    {
        var orchestrator = new Mock<IIntegratorOrchestrator>();
        var sut = new GetSyncStatusHandler(orchestrator.Object);

        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetShipmentStatusHandler

    [Fact]
    public async Task GetShipmentStatusHandler_ValidTracking_ReturnsDto()
    {
        var factory = new Mock<ICargoProviderFactory>();
        var cargoAdapter = new Mock<ICargoAdapter>();
        cargoAdapter.Setup(a => a.TrackShipmentAsync("TRK-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackingResult
            {
                TrackingNumber = "TRK-001",
                Status = CargoStatus.Delivered,
                EstimatedDelivery = DateTime.UtcNow.AddDays(1),
                Events = new List<TrackingEvent>()
            });
        factory.Setup(f => f.Resolve(CargoProvider.YurticiKargo))
            .Returns(cargoAdapter.Object);

        var sut = new GetShipmentStatusHandler(factory.Object);
        var result = await sut.Handle(
            new GetShipmentStatusQuery(TenantId, "TRK-001", CargoProvider.YurticiKargo),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.TrackingNumber.Should().Be("TRK-001");
        result.Status.Should().Be(CargoStatus.Delivered);
    }

    [Fact]
    public async Task GetShipmentStatusHandler_UnknownProvider_ThrowsInvalidOperation()
    {
        var factory = new Mock<ICargoProviderFactory>();
        factory.Setup(f => f.Resolve(It.IsAny<CargoProvider>()))
            .Returns((ICargoAdapter?)null);

        var sut = new GetShipmentStatusHandler(factory.Object);
        var act = () => sut.Handle(
            new GetShipmentStatusQuery(TenantId, "TRK-999", CargoProvider.None),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region GetFulfillmentInventoryHandler

    [Fact]
    public async Task GetFulfillmentInventoryHandler_ValidCenter_ReturnsInventory()
    {
        var factory = new Mock<IFulfillmentProviderFactory>();
        var provider = new Mock<IFulfillmentProvider>();
        provider.Setup(p => p.GetInventoryLevelsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FulfillmentInventory(FulfillmentCenter.AmazonFBA, new List<FulfillmentStock>(), DateTime.UtcNow));
        factory.Setup(f => f.Resolve(FulfillmentCenter.AmazonFBA))
            .Returns(provider.Object);

        var sut = new GetFulfillmentInventoryHandler(factory.Object,
            NullLogger<GetFulfillmentInventoryHandler>.Instance);
        var result = await sut.Handle(
            new GetFulfillmentInventoryQuery(FulfillmentCenter.AmazonFBA, new List<string> { "SKU-1" }),
            CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFulfillmentInventoryHandler_UnknownCenter_ThrowsInvalidOperation()
    {
        var factory = new Mock<IFulfillmentProviderFactory>();
        factory.Setup(f => f.Resolve(It.IsAny<FulfillmentCenter>()))
            .Returns((IFulfillmentProvider?)null);

        var sut = new GetFulfillmentInventoryHandler(factory.Object,
            NullLogger<GetFulfillmentInventoryHandler>.Instance);
        var act = () => sut.Handle(
            new GetFulfillmentInventoryQuery(FulfillmentCenter.AmazonFBA, new List<string> { "SKU-X" }),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region GetFulfillmentOrdersHandler

    [Fact]
    public async Task GetFulfillmentOrdersHandler_ValidCenter_ReturnsOrders()
    {
        var factory = new Mock<IFulfillmentProviderFactory>();
        var provider = new Mock<IFulfillmentProvider>();
        provider.Setup(p => p.GetFulfillmentOrdersAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FulfillmentOrderResult>().AsReadOnly());
        factory.Setup(f => f.Resolve(FulfillmentCenter.AmazonFBA))
            .Returns(provider.Object);

        var sut = new GetFulfillmentOrdersHandler(factory.Object,
            NullLogger<GetFulfillmentOrdersHandler>.Instance);
        var result = await sut.Handle(
            new GetFulfillmentOrdersQuery(FulfillmentCenter.AmazonFBA, DateTime.UtcNow.AddDays(-7)),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region InventoryValuationReportHandler

    [Fact]
    public async Task InventoryValuationReportHandler_EmptyProducts_ReturnsEmptyList()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var sut = new InventoryValuationReportHandler(repo.Object);
        var result = await sut.Handle(
            new InventoryValuationReportQuery(TenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task InventoryValuationReportHandler_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IProductRepository>();
        var sut = new InventoryValuationReportHandler(repo.Object);

        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region OrderFulfillmentReportHandler

    [Fact]
    public async Task OrderFulfillmentReportHandler_EmptyOrders_ReturnsEmptyList()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var sut = new OrderFulfillmentReportHandler(repo.Object);
        var result = await sut.Handle(
            new OrderFulfillmentReportQuery(TenantId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task OrderFulfillmentReportHandler_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IOrderRepository>();
        var sut = new OrderFulfillmentReportHandler(repo.Object);

        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region StockTurnoverReportHandler

    [Fact]
    public async Task StockTurnoverReportHandler_EmptyMovements_ReturnsEmptyList()
    {
        var movementRepo = new Mock<IStockMovementRepository>();
        var productRepo = new Mock<IProductRepository>();

        movementRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.StockMovement>().AsReadOnly());
        productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var sut = new StockTurnoverReportHandler(movementRepo.Object, productRepo.Object);
        var result = await sut.Handle(
            new StockTurnoverReportQuery(TenantId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task StockTurnoverReportHandler_NullRequest_ThrowsArgumentNullException()
    {
        var movementRepo = new Mock<IStockMovementRepository>();
        var productRepo = new Mock<IProductRepository>();
        var sut = new StockTurnoverReportHandler(movementRepo.Object, productRepo.Object);

        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region PlatformPerformanceReportHandler

    [Fact]
    public async Task PlatformPerformanceReportHandler_EmptyOrders_ReturnsEmptyPlatforms()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var commissionRepo = new Mock<ICommissionRecordRepository>();

        orderRepo.Setup(r => r.GetByDateRangeWithItemsAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var sut = new PlatformPerformanceReportHandler(orderRepo.Object, commissionRepo.Object);
        var result = await sut.Handle(
            new PlatformPerformanceReportQuery(TenantId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Platforms.Should().BeEmpty();
    }

    #endregion

    #region PlatformSalesReportHandler

    [Fact]
    public async Task PlatformSalesReportHandler_EmptyOrders_ReturnsEmptyList()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var commissionRepo = new Mock<ICommissionRecordRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();

        orderRepo.Setup(r => r.GetByDateRangeAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());
        settlementRepo.Setup(r => r.GetByDateRangeAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.SettlementBatch>().AsReadOnly());

        var sut = new PlatformSalesReportHandler(orderRepo.Object, commissionRepo.Object, settlementRepo.Object);
        var result = await sut.Handle(
            new PlatformSalesReportQuery(TenantId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PlatformSalesReportHandler_NullRequest_ThrowsArgumentNullException()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var commissionRepo = new Mock<ICommissionRecordRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var sut = new PlatformSalesReportHandler(orderRepo.Object, commissionRepo.Object, settlementRepo.Object);

        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region ProfitabilityReportHandler

    [Fact]
    public async Task ProfitabilityReportHandler_EmptyOrders_ReturnsEmptyReport()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var productRepo = new Mock<IProductRepository>();

        orderRepo.Setup(r => r.GetByDateRangeWithItemsAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var sut = new ProfitabilityReportHandler(orderRepo.Object, productRepo.Object,
            NullLogger<ProfitabilityReportHandler>.Instance);
        var result = await sut.Handle(
            new ProfitabilityReportQuery(TenantId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalOrders.Should().Be(0);
        result.NetProfit.Should().Be(0);
    }

    #endregion
}
