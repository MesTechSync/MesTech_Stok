using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using IOrderRepository = MesTech.Domain.Interfaces.IOrderRepository;
using IProductRepository = MesTech.Domain.Interfaces.IProductRepository;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Concurrency;

/// <summary>
/// Concurrent stock deduction testi.
/// İki eşzamanlı sipariş aynı ürünü düşürürse overselling olmuyor mu?
/// DistributedLock pattern doğrulama.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Concurrency")]
public class ConcurrentStockDeductionTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task TwoOrders_SameProduct_SequentialLock_NoOverselling()
    {
        // Arrange — stok 10, sipariş 1: 7 adet, sipariş 2: 5 adet (toplam 12 > 10)
        var product = new Product
        {
            SKU = "CONC-001", Name = "Concurrent Test", MinimumStock = 0,
            CategoryId = Guid.NewGuid(), TenantId = TenantId
        };
        product.SyncStock(10, "seed");

        var orderId1 = Guid.NewGuid();
        var order1 = new Order { OrderNumber = "ORD-C1", TenantId = TenantId, CustomerId = Guid.NewGuid() };
        order1.AddItem(new OrderItem
        {
            ProductId = product.Id, ProductSKU = "CONC-001", ProductName = "Test",
            Quantity = 7, UnitPrice = 100m, TotalPrice = 700m, TenantId = TenantId
        });

        var orderId2 = Guid.NewGuid();
        var order2 = new Order { OrderNumber = "ORD-C2", TenantId = TenantId, CustomerId = Guid.NewGuid() };
        order2.AddItem(new OrderItem
        {
            ProductId = product.Id, ProductSKU = "CONC-001", ProductName = "Test",
            Quantity = 5, UnitPrice = 100m, TotalPrice = 500m, TenantId = TenantId
        });

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdAsync(orderId1, It.IsAny<CancellationToken>())).ReturnsAsync(order1);
        orderRepo.Setup(r => r.GetByIdAsync(orderId2, It.IsAny<CancellationToken>())).ReturnsAsync(order2);

        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // DistributedLock — serialized access
        var lockSemaphore = new SemaphoreSlim(1, 1);
        var lockService = new Mock<IDistributedLockService>();
        lockService.Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns<string, TimeSpan, TimeSpan, CancellationToken>(async (_, _, _, ct) =>
            {
                await lockSemaphore.WaitAsync(ct);
                return new SemaphoreDisposable(lockSemaphore);
            });

        var handler = new OrderPlacedStockDeductionHandler(
            orderRepo.Object, productRepo.Object, uow.Object, lockService.Object,
            Mock.Of<ILogger<OrderPlacedStockDeductionHandler>>());

        // Act — iki sipariş sıralı (lock serialize eder)
        await handler.HandleAsync(orderId1, "ORD-C1", CancellationToken.None);
        // Sipariş 1: 10 → 3
        product.Stock.Should().Be(3);

        await handler.HandleAsync(orderId2, "ORD-C2", CancellationToken.None);

        // Assert — handler stok yetersizliğinde ikinci siparişi atlar
        // Stok: 10 - 7 = 3, ikinci sipariş 5 adet ister ama 3 < 5 → skip
        // Overselling koruması ÇALIŞIYOR
        product.Stock.Should().Be(3,
            "Handler should skip deduction when stock is insufficient (3 < 5). " +
            "Overselling protection is ACTIVE.");
    }

    [Fact]
    public async Task TwoOrders_SufficientStock_BothDeducted()
    {
        var product = new Product
        {
            SKU = "CONC-OK", Name = "Yeterli Stok", MinimumStock = 0,
            CategoryId = Guid.NewGuid(), TenantId = TenantId
        };
        product.SyncStock(100, "seed");

        var orderId1 = Guid.NewGuid();
        var order1 = new Order { OrderNumber = "ORD-OK1", TenantId = TenantId, CustomerId = Guid.NewGuid() };
        order1.AddItem(new OrderItem
        {
            ProductId = product.Id, ProductSKU = "CONC-OK", ProductName = "Test",
            Quantity = 30, UnitPrice = 10m, TotalPrice = 300m, TenantId = TenantId
        });

        var orderId2 = Guid.NewGuid();
        var order2 = new Order { OrderNumber = "ORD-OK2", TenantId = TenantId, CustomerId = Guid.NewGuid() };
        order2.AddItem(new OrderItem
        {
            ProductId = product.Id, ProductSKU = "CONC-OK", ProductName = "Test",
            Quantity = 40, UnitPrice = 10m, TotalPrice = 400m, TenantId = TenantId
        });

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdAsync(orderId1, It.IsAny<CancellationToken>())).ReturnsAsync(order1);
        orderRepo.Setup(r => r.GetByIdAsync(orderId2, It.IsAny<CancellationToken>())).ReturnsAsync(order2);

        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var lockService = new Mock<IDistributedLockService>();
        lockService.Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());

        var handler = new OrderPlacedStockDeductionHandler(
            orderRepo.Object, productRepo.Object, uow.Object, lockService.Object,
            Mock.Of<ILogger<OrderPlacedStockDeductionHandler>>());

        await handler.HandleAsync(orderId1, "ORD-OK1", CancellationToken.None);
        product.Stock.Should().Be(70, "100 - 30 = 70");

        await handler.HandleAsync(orderId2, "ORD-OK2", CancellationToken.None);
        product.Stock.Should().Be(30, "70 - 40 = 30");
    }

    [Fact]
    public async Task DistributedLock_ShouldBeAcquiredPerProduct()
    {
        var product = new Product
        {
            SKU = "LOCK-001", Name = "Lock Test", MinimumStock = 0,
            CategoryId = Guid.NewGuid(), TenantId = TenantId
        };
        product.SyncStock(50, "seed");

        var orderId = Guid.NewGuid();
        var order = new Order { OrderNumber = "ORD-LOCK", TenantId = TenantId, CustomerId = Guid.NewGuid() };
        order.AddItem(new OrderItem
        {
            ProductId = product.Id, ProductSKU = "LOCK-001", ProductName = "Lock",
            Quantity = 5, UnitPrice = 10m, TotalPrice = 50m, TenantId = TenantId
        });

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var lockService = new Mock<IDistributedLockService>();
        lockService.Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());

        var handler = new OrderPlacedStockDeductionHandler(
            orderRepo.Object, productRepo.Object, uow.Object, lockService.Object,
            Mock.Of<ILogger<OrderPlacedStockDeductionHandler>>());

        await handler.HandleAsync(orderId, "ORD-LOCK", CancellationToken.None);

        lockService.Verify(l => l.AcquireLockAsync(
            It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once, "DistributedLock should be acquired for stock deduction");
    }

    [Fact]
    public async Task ParallelDeduction_WithLock_ShouldSerialize()
    {
        var product = new Product
        {
            SKU = "PAR-001", Name = "Parallel", MinimumStock = 0,
            CategoryId = Guid.NewGuid(), TenantId = TenantId
        };
        product.SyncStock(100, "seed");

        var lockSemaphore = new SemaphoreSlim(1, 1);
        var executionOrder = new List<int>();

        var lockService = new Mock<IDistributedLockService>();
        lockService.Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns<string, TimeSpan, TimeSpan, CancellationToken>(async (_, _, _, ct) =>
            {
                await lockSemaphore.WaitAsync(ct);
                return new SemaphoreDisposable(lockSemaphore);
            });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // 5 sipariş her biri 10 adet
        var tasks = Enumerable.Range(1, 5).Select(async i =>
        {
            var oid = Guid.NewGuid();
            var o = new Order { OrderNumber = $"ORD-PAR-{i}", TenantId = TenantId, CustomerId = Guid.NewGuid() };
            o.AddItem(new OrderItem
            {
                ProductId = product.Id, ProductSKU = "PAR-001", ProductName = "P",
                Quantity = 10, UnitPrice = 10m, TotalPrice = 100m, TenantId = TenantId
            });

            var orderRepo = new Mock<IOrderRepository>();
            orderRepo.Setup(r => r.GetByIdAsync(oid, It.IsAny<CancellationToken>())).ReturnsAsync(o);

            var productRepo = new Mock<IProductRepository>();
            productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product> { product });

            var handler = new OrderPlacedStockDeductionHandler(
                orderRepo.Object, productRepo.Object, uow.Object, lockService.Object,
                Mock.Of<ILogger<OrderPlacedStockDeductionHandler>>());

            await handler.HandleAsync(oid, $"ORD-PAR-{i}", CancellationToken.None);
            lock (executionOrder) { executionOrder.Add(i); }
        });

        await Task.WhenAll(tasks);

        // 100 - (5 × 10) = 50
        product.Stock.Should().Be(50, "5 orders × 10 items = 50 deducted from 100");
        executionOrder.Should().HaveCount(5, "all 5 orders should have been processed");
    }

    [Fact]
    public async Task RowVersion_ProductEntity_ShouldExist()
    {
        // Overselling koruması için ConcurrencyToken gerekli
        var prop = typeof(Product).GetProperty("RowVersion");
        prop.Should().NotBeNull("Product must have RowVersion for optimistic concurrency");
    }

    /// <summary>SemaphoreSlim tabanlı IAsyncDisposable wrapper.</summary>
    private sealed class SemaphoreDisposable : IAsyncDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        public SemaphoreDisposable(SemaphoreSlim semaphore) => _semaphore = semaphore;
        public ValueTask DisposeAsync() { _semaphore.Release(); return ValueTask.CompletedTask; }
    }
}
