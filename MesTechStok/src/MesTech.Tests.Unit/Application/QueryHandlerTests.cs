using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Application.Queries.GetCategories;
using MesTech.Application.Queries.GetInventoryValue;
using MesTech.Application.Queries.GetLowStockProducts;
using MesTech.Application.Queries.GetProductById;
using MesTech.Application.Queries.GetStockMovements;
using MesTech.Application.Queries.GetStoresByTenant;
using MesTech.Application.Queries.GetSuppliers;
using MesTech.Application.Queries.GetSyncStatus;
using MesTech.Application.Queries.GetWarehouses;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application;

// ════════════════════════════════════════════════════════
// 5.D2-02: 9 Query Handler Tests
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
public class GetProductByIdHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();

    private GetProductByIdHandler CreateHandler() => new(_productRepo.Object);

    [Fact]
    public async Task Handle_ExistingProduct_ShouldReturnDto()
    {
        var product = FakeData.CreateProduct(sku: "QRY-001", stock: 50, purchasePrice: 80m, salePrice: 120m);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.SKU.Should().Be("QRY-001");
        result.StockStatus.Should().Be("Normal");
    }

    [Fact]
    public async Task Handle_NotFound_ShouldReturnNull()
    {
        var missingId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetProductByIdQuery(missingId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_OutOfStockProduct_ShouldReturnOutOfStockStatus()
    {
        var product = FakeData.CreateProduct(sku: "OOS-001", stock: 0);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.StockStatus.Should().Be("OutOfStock");
    }
}

[Trait("Category", "Unit")]
public class GetLowStockProductsHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();

    private GetLowStockProductsHandler CreateHandler() => new(_productRepo.Object);

    [Fact]
    public async Task Handle_WithLowStockProducts_ShouldReturnMappedList()
    {
        var products = new List<Product>
        {
            FakeData.CreateProduct(sku: "LOW-A", stock: 2, minimumStock: 10),
            FakeData.CreateProduct(sku: "LOW-B", stock: 1, minimumStock: 5)
        };
        _productRepo.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetLowStockProductsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoLowStockProducts_ShouldReturnEmptyList()
    {
        _productRepo.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetLowStockProductsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryOnce()
    {
        _productRepo.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var handler = CreateHandler();
        await handler.Handle(new GetLowStockProductsQuery(), CancellationToken.None);

        _productRepo.Verify(r => r.GetLowStockAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

[Trait("Category", "Unit")]
public class GetStockMovementsHandlerTests
{
    private readonly Mock<IStockMovementRepository> _movementRepo = new();

    private GetStockMovementsHandler CreateHandler() => new(_movementRepo.Object, Mock.Of<ITenantProvider>());

    [Fact]
    public async Task Handle_ByProductId_ShouldReturnMovements()
    {
        var productId = Guid.NewGuid();
        var movements = new List<StockMovement>
        {
            new() { ProductId = productId, Quantity = 10, Date = DateTime.UtcNow },
            new() { ProductId = productId, Quantity = -5, Date = DateTime.UtcNow }
        };
        _movementRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movements.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetStockMovementsQuery(ProductId: productId), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ByDateRange_ShouldReturnMovements()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var movements = new List<StockMovement>
        {
            new() { Quantity = 20, Date = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc) }
        };
        _movementRepo.Setup(r => r.GetByDateRangeAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movements.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetStockMovementsQuery(From: from, To: to), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NoFilters_ShouldReturnEmpty()
    {
        // Handler now calls GetRecentAsync when no filters specified
        _movementRepo.Setup(r => r.GetRecentAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMovement>().AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStockMovementsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

[Trait("Category", "Unit")]
public class GetInventoryValueHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly StockCalculationService _stockCalc = new();

    private GetInventoryValueHandler CreateHandler() =>
        new(_productRepo.Object, _stockCalc);

    [Fact]
    public async Task Handle_WithProducts_ShouldCalculateCorrectly()
    {
        var products = new List<Product>
        {
            FakeData.CreateProduct(stock: 10, purchasePrice: 100m, minimumStock: 5),
            FakeData.CreateProduct(stock: 0, purchasePrice: 50m, minimumStock: 5),
            FakeData.CreateProduct(stock: 3, purchasePrice: 200m, minimumStock: 5),
        };
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetInventoryValueQuery(), CancellationToken.None);

        result.TotalProducts.Should().Be(3);
        result.TotalStock.Should().Be(13);
        result.TotalValue.Should().Be(10 * 100m + 0 * 50m + 3 * 200m); // 1600
        result.OutOfStockCount.Should().Be(1);
        result.LowStockCount.Should().Be(2); // stock=0 and stock=3 both < min=5
    }

    [Fact]
    public async Task Handle_NoProducts_ShouldReturnZeros()
    {
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetInventoryValueQuery(), CancellationToken.None);

        result.TotalValue.Should().Be(0);
        result.TotalProducts.Should().Be(0);
        result.TotalStock.Should().Be(0);
    }

    [Fact]
    public async Task Handle_AllOutOfStock_ShouldReportCorrectCounts()
    {
        var products = new List<Product>
        {
            FakeData.CreateProduct(stock: 0, minimumStock: 5),
            FakeData.CreateProduct(stock: 0, minimumStock: 10),
        };
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetInventoryValueQuery(), CancellationToken.None);

        result.OutOfStockCount.Should().Be(2);
    }
}

[Trait("Category", "Unit")]
public class GetSyncStatusHandlerTests
{
    private readonly Mock<IIntegratorOrchestrator> _orchestrator = new();

    private GetSyncStatusHandler CreateHandler() => new(_orchestrator.Object);

    [Fact]
    public async Task Handle_AllPlatforms_ShouldReturnAll()
    {
        var adapter1 = new Mock<IIntegratorAdapter>();
        adapter1.Setup(a => a.PlatformCode).Returns("trendyol");
        var adapter2 = new Mock<IIntegratorAdapter>();
        adapter2.Setup(a => a.PlatformCode).Returns("opencart");

        _orchestrator.Setup(o => o.RegisteredAdapters)
            .Returns(new List<IIntegratorAdapter> { adapter1.Object, adapter2.Object });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetSyncStatusQuery(), CancellationToken.None);

        result.Platforms.Should().HaveCount(2);
        result.Platforms.Should().Contain(p => p.PlatformCode == "trendyol");
        result.Platforms.Should().Contain(p => p.PlatformCode == "opencart");
    }

    [Fact]
    public async Task Handle_FilterByPlatform_ShouldReturnSinglePlatform()
    {
        var adapter1 = new Mock<IIntegratorAdapter>();
        adapter1.Setup(a => a.PlatformCode).Returns("trendyol");
        var adapter2 = new Mock<IIntegratorAdapter>();
        adapter2.Setup(a => a.PlatformCode).Returns("opencart");

        _orchestrator.Setup(o => o.RegisteredAdapters)
            .Returns(new List<IIntegratorAdapter> { adapter1.Object, adapter2.Object });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetSyncStatusQuery("trendyol"), CancellationToken.None);

        result.Platforms.Should().HaveCount(1);
        result.Platforms[0].PlatformCode.Should().Be("trendyol");
    }

    [Fact]
    public async Task Handle_NoAdapters_ShouldReturnEmptyList()
    {
        _orchestrator.Setup(o => o.RegisteredAdapters)
            .Returns(new List<IIntegratorAdapter>());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetSyncStatusQuery(), CancellationToken.None);

        result.Platforms.Should().BeEmpty();
    }
}

[Trait("Category", "Unit")]
public class GetStoresByTenantHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepo = new();

    private GetStoresByTenantHandler CreateHandler() => new(_storeRepo.Object);

    [Fact]
    public async Task Handle_WithStores_ShouldReturnMappedDtos()
    {
        var tenantId = Guid.NewGuid();
        var stores = new List<Store>
        {
            FakeData.CreateStore(tenantId, PlatformType.Trendyol),
            FakeData.CreateStore(tenantId, PlatformType.OpenCart),
        };
        _storeRepo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetStoresByTenantQuery(tenantId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(s => s.PlatformType == PlatformType.Trendyol);
    }

    [Fact]
    public async Task Handle_NoStores_ShouldReturnEmpty()
    {
        var tenantId = Guid.NewGuid();
        _storeRepo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>().AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetStoresByTenantQuery(tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldMapFieldsCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var store = FakeData.CreateStore(tenantId, PlatformType.Trendyol);
        _storeRepo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store> { store }.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetStoresByTenantQuery(tenantId), CancellationToken.None);

        var dto = result.Single();
        dto.TenantId.Should().Be(tenantId);
        dto.StoreName.Should().Be(store.StoreName);
        dto.IsActive.Should().BeTrue();
    }
}

[Trait("Category", "Unit")]
public class GetCategoriesHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepo = new();

    private GetCategoriesHandler CreateHandler() => new(_categoryRepo.Object);

    [Fact]
    public async Task Handle_ActiveOnly_ShouldCallGetActive()
    {
        var categories = new List<Category>
        {
            new() { Name = "Electronics", Code = "ELEC", IsActive = true }
        };
        _categoryRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categories.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetCategoriesQuery(ActiveOnly: true), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Electronics");
        _categoryRepo.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _categoryRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AllCategories_ShouldCallGetAll()
    {
        var categories = new List<Category>
        {
            new() { Name = "Active", Code = "ACT", IsActive = true },
            new() { Name = "Inactive", Code = "INA", IsActive = false }
        };
        _categoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categories.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetCategoriesQuery(ActiveOnly: false), CancellationToken.None);

        result.Should().HaveCount(2);
        _categoryRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyCategories_ShouldReturnEmpty()
    {
        _categoryRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>().AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

[Trait("Category", "Unit")]
public class GetSuppliersHandlerTests
{
    private readonly Mock<ISupplierRepository> _supplierRepo = new();

    private GetSuppliersHandler CreateHandler() => new(_supplierRepo.Object);

    [Fact]
    public async Task Handle_ActiveOnly_ShouldReturnActiveSuppliers()
    {
        var suppliers = new List<Supplier>
        {
            new() { Name = "Supplier A", Code = "SA", IsActive = true }
        };
        suppliers[0].MarkAsPreferred();
        suppliers[0].AdjustBalance(1000m);
        _supplierRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(suppliers.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetSuppliersQuery(ActiveOnly: true), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].IsPreferred.Should().BeTrue();
        result[0].CurrentBalance.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_AllSuppliers_ShouldReturnAll()
    {
        var suppliers = new List<Supplier>
        {
            new() { Name = "Active", Code = "A", IsActive = true },
            new() { Name = "Passive", Code = "P", IsActive = false }
        };
        _supplierRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(suppliers.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetSuppliersQuery(ActiveOnly: false), CancellationToken.None);

        result.Should().HaveCount(2);
        _supplierRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Empty_ShouldReturnEmpty()
    {
        _supplierRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Supplier>().AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetSuppliersQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

[Trait("Category", "Unit")]
public class GetWarehousesHandlerTests
{
    private readonly Mock<IWarehouseRepository> _warehouseRepo = new();

    private GetWarehousesHandler CreateHandler() => new(_warehouseRepo.Object);

    [Fact]
    public async Task Handle_ActiveOnly_ShouldFilterInactive()
    {
        var warehouses = new List<Warehouse>
        {
            new() { Name = "Main", Code = "WH1", Type = "MAIN", IsActive = true },
            new() { Name = "Old", Code = "WH2", Type = "AUX", IsActive = false }
        };
        warehouses[0].SetAsDefault();
        _warehouseRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(warehouses.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetWarehousesQuery(ActiveOnly: true), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Main");
        result[0].IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AllWarehouses_ShouldReturnAll()
    {
        var warehouses = new List<Warehouse>
        {
            new() { Name = "WH-A", Code = "A", Type = "MAIN", IsActive = true },
            new() { Name = "WH-B", Code = "B", Type = "COLD", IsActive = false, HasClimateControl = true }
        };
        _warehouseRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(warehouses.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetWarehousesQuery(ActiveOnly: false), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(w => w.HasClimateControl);
    }

    [Fact]
    public async Task Handle_Empty_ShouldReturnEmpty()
    {
        _warehouseRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Warehouse>().AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetWarehousesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
