using FluentAssertions;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application;

// ════════════════════════════════════════════════════════
// Task 19: Dashboard Query Tests (additional scenarios)
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
public class DashboardInventoryStatisticsTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();

    private GetInventoryStatisticsHandler CreateHandler() =>
        new(_productRepo.Object, _movementRepo.Object);

    [Fact]
    public async Task Handle_AllOutOfStock_ReturnsCorrectCounts()
    {
        // Arrange — every product has stock == 0
        var allProducts = new List<Product>
        {
            FakeData.CreateProduct(sku: "OOS-1", stock: 0, salePrice: 500m, minimumStock: 10),
            FakeData.CreateProduct(sku: "OOS-2", stock: 0, salePrice: 250m, minimumStock: 5),
            FakeData.CreateProduct(sku: "OOS-3", stock: 0, salePrice: 100m, minimumStock: 3),
        };

        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(allProducts.AsReadOnly());
        _productRepo.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>())).ReturnsAsync(allProducts.AsReadOnly());
        _movementRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMovement>().AsReadOnly());

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(new GetInventoryStatisticsQuery(), CancellationToken.None);

        // Assert
        result.TotalItems.Should().Be(3);
        result.OutOfStockCount.Should().Be(3);
        result.CriticalStockCount.Should().Be(0); // stock > 0 && <= 5 → none
        result.LowStockCount.Should().Be(0);       // lowStock.Count(p => p.Stock > 5) → none
        result.TotalInventoryValue.Should().Be(0m); // all stock == 0
        result.TodayMovements.Should().Be(0);
    }

    [Fact]
    public async Task Handle_MixedCriticalAndLowStock_CategorizesCorrectly()
    {
        // Arrange
        // Product A: stock=2, critical (>0 && <=5)
        // Product B: stock=4, critical (>0 && <=5)
        // Product C: stock=8, minimumStock=10 → low stock (stock <= minimumStock), stock > 5
        // Product D: stock=100, normal
        var allProducts = new List<Product>
        {
            FakeData.CreateProduct(sku: "CRIT-A", stock: 2, salePrice: 100m, minimumStock: 10),
            FakeData.CreateProduct(sku: "CRIT-B", stock: 4, salePrice: 200m, minimumStock: 10),
            FakeData.CreateProduct(sku: "LOW-C", stock: 8, salePrice: 50m, minimumStock: 10),
            FakeData.CreateProduct(sku: "NORM-D", stock: 100, salePrice: 30m, minimumStock: 10),
        };

        // GetLowStockAsync returns products where stock <= minimumStock
        // A(2<=10), B(4<=10), C(8<=10)
        var lowStockProducts = new List<Product> { allProducts[0], allProducts[1], allProducts[2] };

        var movements = new List<StockMovement>
        {
            new() { Quantity = 10, Date = DateTime.UtcNow },
        };

        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(allProducts.AsReadOnly());
        _productRepo.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>())).ReturnsAsync(lowStockProducts.AsReadOnly());
        _movementRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(movements.AsReadOnly());

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(new GetInventoryStatisticsQuery(), CancellationToken.None);

        // Assert
        result.TotalItems.Should().Be(4);
        result.OutOfStockCount.Should().Be(0);
        result.CriticalStockCount.Should().Be(2); // A(2) and B(4) have stock > 0 && <= 5
        // lowStockProducts.Count(p => p.Stock > 5): C(stock=8) → 1
        result.LowStockCount.Should().Be(1);
        // TotalInventoryValue = 2*100 + 4*200 + 8*50 + 100*30 = 200 + 800 + 400 + 3000 = 4400
        result.TotalInventoryValue.Should().Be(4400m);
        result.TodayMovements.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var act = () => handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
