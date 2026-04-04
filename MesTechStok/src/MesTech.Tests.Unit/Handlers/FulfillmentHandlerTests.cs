using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Features.Accounting.Queries.GetCargoComparison;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentDashboard;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentShipments;
using MesTech.Application.Features.Reports.CargoPerformanceReport;
using MesTech.Application.Features.Reports.FulfillmentCostReport;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Cargo;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for fulfillment and cargo handler queries.
/// </summary>
[Trait("Category", "Unit")]
public class FulfillmentHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════ GetFulfillmentDashboardHandler ═══════

    [Fact]
    public async Task GetFulfillmentDashboard_ReturnsDto()
    {
        var productRepo = new Mock<IProductRepository>();
        var shipmentRepo = new Mock<IFulfillmentShipmentRepository>();
        productRepo.Setup(r => r.CountByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        shipmentRepo.Setup(r => r.CountByTenantAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var sut = new GetFulfillmentDashboardHandler(
            productRepo.Object,
            shipmentRepo.Object,
            NullLogger<GetFulfillmentDashboardHandler>.Instance);

        var result = await sut.Handle(
            new GetFulfillmentDashboardQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalProducts.Should().Be(0);
    }

    [Fact]
    public async Task GetFulfillmentDashboard_NullRequest_ThrowsAnyException()
    {
        var productRepo = new Mock<IProductRepository>();
        var shipmentRepo = new Mock<IFulfillmentShipmentRepository>();
        var sut = new GetFulfillmentDashboardHandler(
            productRepo.Object,
            shipmentRepo.Object,
            NullLogger<GetFulfillmentDashboardHandler>.Instance);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ GetFulfillmentShipmentsHandler ═══════

    [Fact]
    public async Task GetFulfillmentShipments_ReturnsEmptyResult()
    {
        var repo = new Mock<IFulfillmentShipmentRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FulfillmentShipment>());
        repo.Setup(r => r.CountByTenantAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var sut = new GetFulfillmentShipmentsHandler(
            repo.Object,
            NullLogger<GetFulfillmentShipmentsHandler>.Instance);

        var result = await sut.Handle(
            new GetFulfillmentShipmentsQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFulfillmentShipments_NullRequest_ThrowsAnyException()
    {
        var sut = new GetFulfillmentShipmentsHandler(
            Mock.Of<IFulfillmentShipmentRepository>(),
            NullLogger<GetFulfillmentShipmentsHandler>.Instance);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ FulfillmentCostReportHandler ═══════

    [Fact]
    public async Task FulfillmentCostReport_NoProviders_ReturnsEmptyReport()
    {
        var factory = new Mock<IFulfillmentProviderFactory>();
        factory.Setup(f => f.Resolve(It.IsAny<MesTech.Application.DTOs.Fulfillment.FulfillmentCenter>()))
            .Returns((IFulfillmentProvider?)null);

        var sut = new FulfillmentCostReportHandler(
            factory.Object,
            NullLogger<FulfillmentCostReportHandler>.Instance);

        var query = new FulfillmentCostReportQuery(
            _tenantId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalFulfillmentCost.Should().Be(0);
    }

    [Fact]
    public async Task FulfillmentCostReport_NullRequest_ThrowsAnyException()
    {
        var factory = new Mock<IFulfillmentProviderFactory>();
        var sut = new FulfillmentCostReportHandler(
            factory.Object,
            NullLogger<FulfillmentCostReportHandler>.Instance);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ GetCargoComparisonHandler ═══════

    [Fact]
    public async Task GetCargoComparison_NullRequest_ThrowsArgumentNullException()
    {
        var factory = new Mock<ICargoProviderFactory>();
        var sut = new GetCargoComparisonHandler(
            factory.Object,
            NullLogger<GetCargoComparisonHandler>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetCargoComparison_NoAdapters_ReturnsEmptyResult()
    {
        var factory = new Mock<ICargoProviderFactory>();
        factory.Setup(f => f.GetAll()).Returns(new List<ICargoAdapter>().AsReadOnly());

        var sut = new GetCargoComparisonHandler(
            factory.Object,
            NullLogger<GetCargoComparisonHandler>.Instance);

        var query = new GetCargoComparisonQuery(new ShipmentRequest());

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.CheapestProvider.Should().BeNull();
        result.FastestProvider.Should().BeNull();
    }

    // ═══════ CargoPerformanceReportHandler ═══════

    [Fact]
    public async Task CargoPerformanceReport_NullRequest_ThrowsArgumentNullException()
    {
        var cargoExpenseRepo = new Mock<ICargoExpenseRepository>();
        var orderRepo = new Mock<IOrderRepository>();
        var sut = new CargoPerformanceReportHandler(cargoExpenseRepo.Object, orderRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CargoPerformanceReport_EmptyData_ReturnsEmptyList()
    {
        var cargoExpenseRepo = new Mock<ICargoExpenseRepository>();
        var orderRepo = new Mock<IOrderRepository>();

        cargoExpenseRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.CargoExpense>().AsReadOnly());

        orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var sut = new CargoPerformanceReportHandler(cargoExpenseRepo.Object, orderRepo.Object);
        var query = new CargoPerformanceReportQuery(_tenantId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
