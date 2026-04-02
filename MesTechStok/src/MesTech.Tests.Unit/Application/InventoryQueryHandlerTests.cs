using FluentAssertions;
using MesTech.Application.Queries.GetInventoryPaged;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application;

// ════════════════════════════════════════════════════════
// Task 9: Inventory Query Handler Tests
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
public class GetInventoryPagedHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<ICategoryRepository> _categoryRepo = new();

    private GetInventoryPagedHandler CreateHandler() =>
        new(_productRepo.Object, _categoryRepo.Object);

    private void SetupEmptyCategories()
    {
        _categoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>().AsReadOnly());
    }

    [Fact]
    public async Task Handle_ReturnsPagedProducts()
    {
        // Arrange
        var products = Enumerable.Range(1, 5)
            .Select(i => FakeData.CreateProduct(sku: $"INV-{i:D3}", stock: i * 10, minimumStock: 3))
            .ToList();
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products.AsReadOnly());
        SetupEmptyCategories();

        // Act — request page 1 with size 3
        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetInventoryPagedQuery(Page: 1, PageSize: 3), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalItems.Should().Be(5);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(3);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_UsesSearchAsync()
    {
        // Arrange
        var products = new List<Product>
        {
            FakeData.CreateProduct(sku: "SEARCH-001", stock: 20, minimumStock: 3)
        };
        _productRepo.Setup(r => r.SearchAsync("SEARCH"))
            .ReturnsAsync(products.AsReadOnly());
        SetupEmptyCategories();

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetInventoryPagedQuery(SearchTerm: "SEARCH"), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        _productRepo.Verify(r => r.SearchAsync("SEARCH"), Times.Once);
        _productRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StatusFilter_OutOfStock_FiltersCorrectly()
    {
        // Arrange — mix of in-stock and out-of-stock products
        var products = new List<Product>
        {
            FakeData.CreateProduct(sku: "OOS-001", stock: 0, minimumStock: 5),
            FakeData.CreateProduct(sku: "OOS-002", stock: 0, minimumStock: 10),
            FakeData.CreateProduct(sku: "IN-001", stock: 50, minimumStock: 5),
        };
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products.AsReadOnly());
        SetupEmptyCategories();

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetInventoryPagedQuery(StatusFilter: StockStatusFilter.OutOfStock),
            CancellationToken.None);

        // Assert — only the 2 out-of-stock products
        result.TotalItems.Should().Be(2);
        result.Items.Should().OnlyContain(i => i.Stock == 0);
    }

    [Fact]
    public async Task Handle_SortOrder_StockDesc_SortsCorrectly()
    {
        // Arrange
        var products = new List<Product>
        {
            FakeData.CreateProduct(sku: "SORT-A", stock: 10, minimumStock: 3),
            FakeData.CreateProduct(sku: "SORT-B", stock: 100, minimumStock: 3),
            FakeData.CreateProduct(sku: "SORT-C", stock: 50, minimumStock: 3),
        };
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products.AsReadOnly());
        SetupEmptyCategories();

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetInventoryPagedQuery(SortOrder: InventorySortOrder.StockDesc),
            CancellationToken.None);

        // Assert — descending by stock: 100, 50, 10
        result.Items.Should().HaveCount(3);
        result.Items[0].Stock.Should().Be(100);
        result.Items[1].Stock.Should().Be(50);
        result.Items[2].Stock.Should().Be(10);
    }
}

[Trait("Category", "Unit")]
public class GetInventoryStatisticsHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();

    private GetInventoryStatisticsHandler CreateHandler() =>
        new(_productRepo.Object, _movementRepo.Object);

    [Fact]
    public async Task Handle_ReturnsCorrectStatistics()
    {
        // Arrange
        // Product A: stock=0 (out of stock), salePrice=100
        // Product B: stock=3 (critical: >0 && <=5), salePrice=200
        // Product C: stock=50, minimumStock=10 (normal), salePrice=50
        var allProducts = new List<Product>
        {
            FakeData.CreateProduct(sku: "STAT-A", stock: 0, salePrice: 100m, minimumStock: 10),
            FakeData.CreateProduct(sku: "STAT-B", stock: 3, salePrice: 200m, minimumStock: 10),
            FakeData.CreateProduct(sku: "STAT-C", stock: 50, salePrice: 50m, minimumStock: 10),
        };
        // GetLowStockAsync returns products where stock <= minimumStock
        // B (stock=3 <= min=10) and A (stock=0 <= min=10)
        var lowStockProducts = new List<Product> { allProducts[0], allProducts[1] };

        var todayMovements = new List<StockMovement>
        {
            new() { Quantity = 10, Date = DateTime.UtcNow },
            new() { Quantity = -5, Date = DateTime.UtcNow },
            new() { Quantity = 20, Date = DateTime.UtcNow },
        };

        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(allProducts.AsReadOnly());
        _productRepo.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>())).ReturnsAsync(lowStockProducts.AsReadOnly());
        _movementRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(todayMovements.AsReadOnly());

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(new GetInventoryStatisticsQuery(), CancellationToken.None);

        // Assert
        // TotalInventoryValue = sum(stock * salePrice) = 0*100 + 3*200 + 50*50 = 0 + 600 + 2500 = 3100
        result.TotalInventoryValue.Should().Be(3100m);
        result.TotalItems.Should().Be(3);
        // OutOfStock: stock == 0 → 1 product (STAT-A)
        result.OutOfStockCount.Should().Be(1);
        // Critical: stock > 0 && stock <= 5 → 1 product (STAT-B, stock=3)
        result.CriticalStockCount.Should().Be(1);
        // LowStockCount from handler: lowStockProducts.Count(p => p.Stock > 5)
        // lowStockProducts are A(stock=0) and B(stock=3), neither has stock>5
        result.LowStockCount.Should().Be(0);
        result.TodayMovements.Should().Be(3);
    }

    [Fact]
    public async Task Handle_NoProducts_ReturnsZeros()
    {
        // Arrange
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());
        _productRepo.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());
        _movementRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMovement>().AsReadOnly());

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(new GetInventoryStatisticsQuery(), CancellationToken.None);

        // Assert
        result.TotalInventoryValue.Should().Be(0);
        result.TotalItems.Should().Be(0);
        result.LowStockCount.Should().Be(0);
        result.CriticalStockCount.Should().Be(0);
        result.OutOfStockCount.Should().Be(0);
        result.TodayMovements.Should().Be(0);
    }
}
