using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Application.Queries.GetInventoryValue;
using MesTech.Application.Queries.GetLowStockProducts;
using MesTech.Application.Queries.GetStockMovements;
using MesTech.Application.Queries.GetSyncStatus;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application;

/// <summary>
/// 5-04 coverage gap-fill: GetInventoryValue, GetLowStockProducts,
/// GetStockMovements, GetSyncStatus handler testleri.
/// </summary>
[Trait("Category", "Unit")]
public class GapFillQueryHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IIntegratorOrchestrator> _orchestrator = new();

    // StockCalculationService is a pure domain class — use real instance, no mock needed.
    private readonly StockCalculationService _stockCalc = new();

    private GetInventoryValueHandler InventoryHandler() =>
        new(_productRepo.Object, _stockCalc);

    private GetLowStockProductsHandler LowStockHandler() =>
        new(_productRepo.Object);

    private GetStockMovementsHandler MovementsHandler() =>
        new(_movementRepo.Object, Mock.Of<ITenantProvider>());

    private GetSyncStatusHandler SyncStatusHandler() =>
        new(_orchestrator.Object);

    // ── GetInventoryValue ──

    [Fact]
    public async Task GetInventoryValue_TwoProducts_CalculatesTotalValue()
    {
        // Stock * PurchasePrice = inventory value
        var p1 = FakeData.CreateProduct(stock: 10, purchasePrice: 50m);   // value = 500
        var p2 = FakeData.CreateProduct(stock: 5,  purchasePrice: 100m);  // value = 500
        _productRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Product> { p1, p2 });

        var result = await InventoryHandler().Handle(
            new GetInventoryValueQuery(), CancellationToken.None);

        result.TotalValue.Should().Be(1000m);
        result.TotalProducts.Should().Be(2);
        result.TotalStock.Should().Be(15);
    }

    [Fact]
    public async Task GetInventoryValue_EmptyRepo_ReturnsZeros()
    {
        _productRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Product>());

        var result = await InventoryHandler().Handle(
            new GetInventoryValueQuery(), CancellationToken.None);

        result.TotalValue.Should().Be(0m);
        result.TotalProducts.Should().Be(0);
        result.TotalStock.Should().Be(0);
        result.LowStockCount.Should().Be(0);
        result.OutOfStockCount.Should().Be(0);
    }

    [Fact]
    public async Task GetInventoryValue_LowStockAndOutOfStock_CountedCorrectly()
    {
        // MinimumStock = 5 by FakeData default
        var outOfStock = FakeData.CreateProduct(stock: 0,  minimumStock: 5);  // IsOutOfStock
        var lowStock   = FakeData.CreateProduct(stock: 3,  minimumStock: 5);  // IsLowStock
        var normal     = FakeData.CreateProduct(stock: 50, minimumStock: 5);

        _productRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Product> { outOfStock, lowStock, normal });

        var result = await InventoryHandler().Handle(
            new GetInventoryValueQuery(), CancellationToken.None);

        result.TotalProducts.Should().Be(3);
        result.OutOfStockCount.Should().Be(1);
        result.LowStockCount.Should().BeGreaterThanOrEqualTo(1,
            "stock=3 < minimumStock=5 so IsLowStock() must be true");
    }

    // ── GetLowStockProducts ──

    [Fact]
    public async Task GetLowStockProducts_RepoReturns2_Returns2Dtos()
    {
        var p1 = FakeData.CreateProduct(sku: "LOW-001", stock: 2, minimumStock: 5);
        var p2 = FakeData.CreateProduct(sku: "LOW-002", stock: 0, minimumStock: 5);
        _productRepo.Setup(r => r.GetLowStockAsync())
            .ReturnsAsync(new List<Product> { p1, p2 });

        var result = await LowStockHandler().Handle(
            new GetLowStockProductsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(r => r.SKU).Should().Contain("LOW-001").And.Contain("LOW-002");
    }

    [Fact]
    public async Task GetLowStockProducts_EmptyRepo_ReturnsEmpty()
    {
        _productRepo.Setup(r => r.GetLowStockAsync())
            .ReturnsAsync(new List<Product>());

        var result = await LowStockHandler().Handle(
            new GetLowStockProductsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
        _productRepo.Verify(r => r.GetLowStockAsync(), Times.Once);
    }

    // ── GetStockMovements ──

    [Fact]
    public async Task GetStockMovements_FilterByProductId_CallsGetByProductId()
    {
        var productId = Guid.NewGuid();
        var movements = new List<StockMovement>
        {
            new() { ProductId = productId, Quantity = 10, MovementType = StockMovementType.StockIn.ToString() },
            new() { ProductId = productId, Quantity = -3, MovementType = StockMovementType.StockOut.ToString() }
        };
        _movementRepo.Setup(r => r.GetByProductIdAsync(productId))
            .ReturnsAsync(movements);

        var result = await MovementsHandler().Handle(
            new GetStockMovementsQuery(ProductId: productId), CancellationToken.None);

        result.Should().HaveCount(2);
        _movementRepo.Verify(r => r.GetByProductIdAsync(productId), Times.Once);
        _movementRepo.Verify(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task GetStockMovements_FilterByDateRange_CallsGetByDateRange()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to   = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        _movementRepo.Setup(r => r.GetByDateRangeAsync(from, to))
            .ReturnsAsync(new List<StockMovement>
            {
                new() { MovementType = StockMovementType.StockIn.ToString(), Quantity = 20 }
            });

        var result = await MovementsHandler().Handle(
            new GetStockMovementsQuery(From: from, To: to), CancellationToken.None);

        result.Should().HaveCount(1);
        _movementRepo.Verify(r => r.GetByDateRangeAsync(from, to), Times.Once);
    }

    [Fact]
    public async Task GetStockMovements_NoFilter_ReturnsEmpty()
    {
        // Handler returns empty list when no filter specified — by design
        var result = await MovementsHandler().Handle(
            new GetStockMovementsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
        _movementRepo.Verify(r => r.GetByProductIdAsync(It.IsAny<Guid>()), Times.Never);
        _movementRepo.Verify(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    // ── GetSyncStatus ──

    [Fact]
    public async Task GetSyncStatus_TwoAdapters_ReturnsTwoPlatforms()
    {
        var adapter1 = new Mock<IIntegratorAdapter>();
        adapter1.Setup(a => a.PlatformCode).Returns("TRENDYOL");
        var adapter2 = new Mock<IIntegratorAdapter>();
        adapter2.Setup(a => a.PlatformCode).Returns("OPENCART");

        _orchestrator.Setup(o => o.RegisteredAdapters)
            .Returns(new List<IIntegratorAdapter> { adapter1.Object, adapter2.Object });

        var result = await SyncStatusHandler().Handle(
            new GetSyncStatusQuery(), CancellationToken.None);

        result.Platforms.Should().HaveCount(2);
        result.Platforms.Select(p => p.PlatformCode)
            .Should().Contain("TRENDYOL").And.Contain("OPENCART");
        result.Platforms.Should().AllSatisfy(p => p.IsEnabled.Should().BeTrue());
    }

    [Fact]
    public async Task GetSyncStatus_FilterByPlatformCode_ReturnsOnlyMatching()
    {
        var adapter1 = new Mock<IIntegratorAdapter>();
        adapter1.Setup(a => a.PlatformCode).Returns("TRENDYOL");
        var adapter2 = new Mock<IIntegratorAdapter>();
        adapter2.Setup(a => a.PlatformCode).Returns("OPENCART");

        _orchestrator.Setup(o => o.RegisteredAdapters)
            .Returns(new List<IIntegratorAdapter> { adapter1.Object, adapter2.Object });

        var result = await SyncStatusHandler().Handle(
            new GetSyncStatusQuery(PlatformCode: "TRENDYOL"), CancellationToken.None);

        result.Platforms.Should().HaveCount(1);
        result.Platforms[0].PlatformCode.Should().Be("TRENDYOL");
    }

    [Fact]
    public async Task GetSyncStatus_NoAdapters_ReturnsEmptyPlatforms()
    {
        _orchestrator.Setup(o => o.RegisteredAdapters)
            .Returns(new List<IIntegratorAdapter>());

        var result = await SyncStatusHandler().Handle(
            new GetSyncStatusQuery(), CancellationToken.None);

        result.Platforms.Should().BeEmpty();
    }
}
