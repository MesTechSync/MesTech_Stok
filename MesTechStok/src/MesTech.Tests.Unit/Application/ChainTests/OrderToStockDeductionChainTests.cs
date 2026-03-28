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
    private readonly Mock<ILogger<OrderPlacedStockDeductionHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_WhenOrderPlaced_ShouldDeductStockFromProducts()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var product = new Product
        {
            SKU = "TST-001",
            Name = "Test Urun",
            Stock = 100,
            MinimumStock = 5,
            CategoryId = Guid.NewGuid(),
            TenantId = Guid.NewGuid()
        };

        var orderItem = new OrderItem
        {
            ProductId = productId,
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

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _productRepoMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new OrderPlacedStockDeductionHandler(
            _orderRepoMock.Object,
            _productRepoMock.Object,
            _unitOfWorkMock.Object,
            Mock.Of<IDistributedLockService>(),
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
        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

        var handler = new OrderPlacedStockDeductionHandler(
            _orderRepoMock.Object,
            _productRepoMock.Object,
            _unitOfWorkMock.Object,
            Mock.Of<IDistributedLockService>(),
            _loggerMock.Object);

        // Act & Assert — handler gracefully returns, does not throw
        await handler.Invoking(h => h.HandleAsync(orderId, "ORD-MISSING", CancellationToken.None))
            .Should().NotThrowAsync();

        _productRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMultipleItems_ShouldDeductAllProducts()
    {
        // Arrange
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var product1 = new Product { SKU = "P1", Name = "Urun 1", Stock = 50, MinimumStock = 5, CategoryId = Guid.NewGuid(), TenantId = tenantId };
        var product2 = new Product { SKU = "P2", Name = "Urun 2", Stock = 30, MinimumStock = 5, CategoryId = Guid.NewGuid(), TenantId = tenantId };

        var order = new Order { OrderNumber = "ORD-002", TenantId = tenantId, CustomerId = Guid.NewGuid() };
        order.AddItem(new OrderItem { ProductId = product1Id, ProductSKU = "P1", ProductName = "Urun 1", Quantity = 5, UnitPrice = 10, TotalPrice = 50, TenantId = tenantId });
        order.AddItem(new OrderItem { ProductId = product2Id, ProductSKU = "P2", ProductName = "Urun 2", Quantity = 10, UnitPrice = 20, TotalPrice = 200, TenantId = tenantId });

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _productRepoMock.Setup(r => r.GetByIdAsync(product1Id)).ReturnsAsync(product1);
        _productRepoMock.Setup(r => r.GetByIdAsync(product2Id)).ReturnsAsync(product2);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new OrderPlacedStockDeductionHandler(
            _orderRepoMock.Object, _productRepoMock.Object,
            _unitOfWorkMock.Object, _loggerMock.Object);

        // Act
        await handler.HandleAsync(orderId, "ORD-002", CancellationToken.None);

        // Assert
        product1.Stock.Should().Be(45); // 50 - 5
        product2.Stock.Should().Be(20); // 30 - 10
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
