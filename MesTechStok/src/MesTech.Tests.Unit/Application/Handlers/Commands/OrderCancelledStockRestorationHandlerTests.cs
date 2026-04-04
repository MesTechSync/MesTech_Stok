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
public class OrderCancelledStockRestorationHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDistributedLockService> _lockServiceMock = new();
    private readonly Mock<ILogger<OrderCancelledStockRestorationHandler>> _loggerMock = new();

    private OrderCancelledStockRestorationHandler CreateHandler() =>
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
        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "Test reason", CancellationToken.None);

        _productRepoMock.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_HappyPath_ShouldRestoreStockAndSave()
    {
        var product = FakeData.CreateProduct(sku: "SKU-RESTORE", stock: 10);
        var order = FakeData.CreateOrder();
        var item = new OrderItem
        {
            ProductId = product.Id,
            ProductSKU = "SKU-RESTORE",
            ProductName = "Restore Product",
            TaxRate = 0.18m
        };
        item.SetQuantityAndPrice(5, 100m);
        order.AddItem(item);

        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _lockServiceMock.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLockHandle().Object);
        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var handler = CreateHandler();
        await handler.HandleAsync(order.Id, order.TenantId, "Customer cancelled", CancellationToken.None);

        product.Stock.Should().Be(15);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_LockFailed_ShouldReturnEarlyWithoutRestoring()
    {
        var order = FakeData.CreateOrder();
        var item = new OrderItem
        {
            ProductId = Guid.NewGuid(),
            ProductSKU = "SKU-NOLOCK",
            ProductName = "No Lock",
            TaxRate = 0.18m
        };
        item.SetQuantityAndPrice(2, 30m);
        order.AddItem(item);

        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _lockServiceMock.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable?)null);

        var handler = CreateHandler();
        await handler.HandleAsync(order.Id, order.TenantId, null, CancellationToken.None);

        _productRepoMock.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NullReason_ShouldNotThrow()
    {
        var product = FakeData.CreateProduct(sku: "SKU-NULL-R", stock: 5);
        var order = FakeData.CreateOrder();
        var item = new OrderItem
        {
            ProductId = product.Id,
            ProductSKU = "SKU-NULL-R",
            ProductName = "Null Reason",
            TaxRate = 0.18m
        };
        item.SetQuantityAndPrice(1, 20m);
        order.AddItem(item);

        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _lockServiceMock.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLockHandle().Object);
        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var handler = CreateHandler();

        var act = () => handler.HandleAsync(order.Id, order.TenantId, null, CancellationToken.None);

        await act.Should().NotThrowAsync();
        product.Stock.Should().Be(6);
    }

    [Fact]
    public async Task HandleAsync_ProductNotFound_ShouldContinueWithOtherItems()
    {
        var product = FakeData.CreateProduct(sku: "SKU-FOUND", stock: 20);
        var order = FakeData.CreateOrder();

        var missingItem = new OrderItem
        {
            ProductId = Guid.NewGuid(),
            ProductSKU = "SKU-GONE",
            ProductName = "Gone Product",
            TaxRate = 0.18m
        };
        missingItem.SetQuantityAndPrice(3, 10m);

        var foundItem = new OrderItem
        {
            ProductId = product.Id,
            ProductSKU = "SKU-FOUND",
            ProductName = "Found Product",
            TaxRate = 0.18m
        };
        foundItem.SetQuantityAndPrice(4, 50m);

        order.AddItem(missingItem);
        order.AddItem(foundItem);

        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _lockServiceMock.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLockHandle().Object);
        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var handler = CreateHandler();
        await handler.HandleAsync(order.Id, order.TenantId, "Partial restore", CancellationToken.None);

        product.Stock.Should().Be(24);
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
            ProductSKU = "SKU-EX",
            ProductName = "Exception Product",
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
        var act = () => handler.HandleAsync(order.Id, order.TenantId, "reason", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        lockHandle.Verify(h => h.DisposeAsync(), Times.Once);
    }
}
