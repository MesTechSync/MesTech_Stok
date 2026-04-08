using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class OrderPlacedStockDeductionHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDistributedLockService> _lockServiceMock = new();
    private readonly Mock<ILogger<OrderPlacedStockDeductionHandler>> _loggerMock = new();

    private OrderPlacedStockDeductionHandler CreateHandler() =>
        new(_orderRepoMock.Object, _productRepoMock.Object, _uowMock.Object,
            _lockServiceMock.Object, _loggerMock.Object);

    private static Mock<IAsyncDisposable> CreateLockHandle()
    {
        var handle = new Mock<IAsyncDisposable>();
        handle.Setup(h => h.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return handle;
    }

    [Fact]
    public async Task HandleAsync_OrderNotFound_ShouldReturnEarly()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var handler = CreateHandler();
        await handler.HandleAsync(Guid.NewGuid(), "ORD-999", CancellationToken.None);

        _productRepoMock.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_HappyPath_ShouldDeductStockAndSave()
    {
        var product = FakeData.CreateProduct(sku: "SKU-A", stock: 50);
        var order = FakeData.CreateOrder();
        var item = new OrderItem
        {
            ProductId = product.Id,
            ProductSKU = "SKU-A",
            ProductName = "Test Product",
            TaxRate = 0.18m
        };
        item.SetQuantityAndPrice(3, 100m);
        order.AddItem(item);

        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _lockServiceMock.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLockHandle().Object);
        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var handler = CreateHandler();
        await handler.HandleAsync(order.Id, order.OrderNumber, CancellationToken.None);

        product.Stock.Should().Be(47);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_LockFailed_ShouldReturnEarlyWithoutDeducting()
    {
        var order = FakeData.CreateOrder();
        var item = new OrderItem
        {
            ProductId = Guid.NewGuid(),
            ProductSKU = "SKU-LOCK",
            ProductName = "Lock Test",
            TaxRate = 0.18m
        };
        item.SetQuantityAndPrice(1, 50m);
        order.AddItem(item);

        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _lockServiceMock.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable?)null);

        var handler = CreateHandler();
        await handler.HandleAsync(order.Id, order.OrderNumber, CancellationToken.None);

        _productRepoMock.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ProductNotFound_ShouldContinueWithOtherItems()
    {
        var product = FakeData.CreateProduct(sku: "SKU-EXISTS", stock: 20);
        var order = FakeData.CreateOrder();

        var missingItem = new OrderItem
        {
            ProductId = Guid.NewGuid(),
            ProductSKU = "SKU-MISSING",
            ProductName = "Missing",
            TaxRate = 0.18m
        };
        missingItem.SetQuantityAndPrice(1, 10m);

        var existingItem = new OrderItem
        {
            ProductId = product.Id,
            ProductSKU = "SKU-EXISTS",
            ProductName = "Existing",
            TaxRate = 0.18m
        };
        existingItem.SetQuantityAndPrice(2, 50m);

        order.AddItem(missingItem);
        order.AddItem(existingItem);

        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _lockServiceMock.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLockHandle().Object);
        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var handler = CreateHandler();
        await handler.HandleAsync(order.Id, order.OrderNumber, CancellationToken.None);

        product.Stock.Should().Be(18);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReleaseLocks_EvenOnException()
    {
        var lockHandle = CreateLockHandle();
        var order = FakeData.CreateOrder();
        var item = new OrderItem
        {
            ProductId = Guid.NewGuid(),
            ProductSKU = "SKU-ERR",
            ProductName = "Error Product",
            TaxRate = 0.18m
        };
        item.SetQuantityAndPrice(1, 10m);
        order.AddItem(item);

        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _lockServiceMock.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockHandle.Object);
        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var handler = CreateHandler();
        var act = () => handler.HandleAsync(order.Id, order.OrderNumber, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        lockHandle.Verify(h => h.DisposeAsync(), Times.Once);
    }
}
