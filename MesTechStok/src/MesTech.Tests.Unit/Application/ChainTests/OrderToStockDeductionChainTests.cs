using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Entities;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Tests.Unit.Application.ChainTests;

/// <summary>
/// Zincir 1 E2E: PlaceOrder -> OrderPlacedStockDeductionHandler -> stok azaldi
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "ChainE2E")]
public class OrderToStockDeductionChainTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDistributedLockService> _lockServiceMock = new();
    private readonly Mock<ILogger<OrderPlacedStockDeductionHandler>> _loggerMock = new();

    public OrderToStockDeductionChainTests()
    {
        // Default lock mock — always grants lock
        _lockServiceMock
            .Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());
    }

    [Fact]
    public async Task Handle_WhenOrderPlaced_ShouldDeductStockFromProducts()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var product = new Product
        {
            SKU = "TST-001",
            Name = "Test Urun",
            MinimumStock = 5,
            CategoryId = Guid.NewGuid(),
            TenantId = Guid.NewGuid()
        };
        product.SyncStock(100, "test-seed");

        var orderItem = new OrderItem
        {
            ProductId = product.Id,
            ProductSKU = "TST-001",
            ProductName = "Test Urun",
            Quantity = 3,
            UnitPrice = 50m,
            TotalPrice = 150m,
            TenantId = Guid.NewGuid()
        };

        var order = new Order
        {
            OrderNumber = "ORD-001",
            TenantId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid()
        };
        order.AddItem(orderItem);

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new OrderPlacedStockDeductionHandler(
            _orderRepoMock.Object,
            _productRepoMock.Object,
            _unitOfWorkMock.Object,
            _lockServiceMock.Object,
            _loggerMock.Object);

        // Act
        await handler.HandleAsync(orderId, "ORD-001", CancellationToken.None);

        // Assert — stok 100'den 97'ye dustu
        product.Stock.Should().Be(97);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ShouldNotThrow()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var handler = new OrderPlacedStockDeductionHandler(
            _orderRepoMock.Object,
            _productRepoMock.Object,
            _unitOfWorkMock.Object,
            _lockServiceMock.Object,
            _loggerMock.Object);

        // Act & Assert — handler gracefully returns, does not throw
        await handler.Invoking(h => h.HandleAsync(orderId, "ORD-MISSING", CancellationToken.None))
            .Should().NotThrowAsync();

        _productRepoMock.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMultipleItems_ShouldDeductAllProducts()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var product1 = new Product { SKU = "P1", Name = "Urun 1", MinimumStock = 5, CategoryId = Guid.NewGuid(), TenantId = tenantId };
        product1.SyncStock(50, "test-seed");
        var product2 = new Product { SKU = "P2", Name = "Urun 2", MinimumStock = 5, CategoryId = Guid.NewGuid(), TenantId = tenantId };
        product2.SyncStock(30, "test-seed");

        var order = new Order { OrderNumber = "ORD-002", TenantId = tenantId, CustomerId = Guid.NewGuid() };
        order.AddItem(new OrderItem { ProductId = product1.Id, ProductSKU = "P1", ProductName = "Urun 1", Quantity = 5, UnitPrice = 10, TotalPrice = 50, TenantId = tenantId });
        order.AddItem(new OrderItem { ProductId = product2.Id, ProductSKU = "P2", ProductName = "Urun 2", Quantity = 10, UnitPrice = 20, TotalPrice = 200, TenantId = tenantId });

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product1, product2 });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new OrderPlacedStockDeductionHandler(
            _orderRepoMock.Object, _productRepoMock.Object,
            _unitOfWorkMock.Object, _lockServiceMock.Object, _loggerMock.Object);

        // Act
        await handler.HandleAsync(orderId, "ORD-002", CancellationToken.None);

        // Assert
        product1.Stock.Should().Be(45); // 50 - 5
        product2.Stock.Should().Be(20); // 30 - 10
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
