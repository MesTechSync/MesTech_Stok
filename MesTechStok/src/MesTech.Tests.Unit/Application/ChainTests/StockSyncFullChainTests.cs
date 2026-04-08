using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using IProductPlatformMappingRepository = MesTech.Domain.Interfaces.IProductPlatformMappingRepository;
using ISyncLogRepository = MesTech.Domain.Interfaces.ISyncLogRepository;
using IOrderRepository = MesTech.Domain.Interfaces.IOrderRepository;
using IProductRepository = MesTech.Domain.Interfaces.IProductRepository;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.ChainTests;

/// <summary>
/// TEST 4/4 — StockSync full chain: Product.Stock değişti → StockChangedEvent → PlatformSync → adapter çağrıldı.
/// OrderPlacedStockDeduction (Z1) + StockChangedPlatformSync (Z9) zincirini birleştirir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "ChainE2E")]
public class StockSyncFullChainTests
{
    // Z1 dependencies
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDistributedLockService> _lock = new();
    private readonly Mock<ILogger<OrderPlacedStockDeductionHandler>> _logZ1 = new();

    // Z9 dependencies
    private readonly Mock<IProductPlatformMappingRepository> _mappingRepo = new();
    private readonly Mock<IAdapterFactory> _factory = new();
    private readonly Mock<ISyncLogRepository> _syncLogRepo = new();
    private readonly Mock<ILogger<StockChangedPlatformSyncHandler>> _logZ9 = new();

    public StockSyncFullChainTests()
    {
        _lock.Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    // ══════════════════════════════════════
    // Full chain: Order → Stock düş → PlatformSync
    // ══════════════════════════════════════

    [Fact]
    public async Task FullChain_OrderPlaced_StockDeducted_ThenPlatformSynced()
    {
        // ── Z1 SETUP ──
        var product = new Product
        {
            SKU = "CHAIN-001", Name = "Zincir Urun", MinimumStock = 5,
            CategoryId = Guid.NewGuid(), TenantId = Guid.NewGuid()
        };
        product.SyncStock(100, "seed");

        var orderId = Guid.NewGuid();
        var order = new Order
        {
            OrderNumber = "ORD-CHAIN-001", TenantId = product.TenantId, CustomerId = Guid.NewGuid()
        };
        order.AddItem(new OrderItem
        {
            ProductId = product.Id, ProductSKU = "CHAIN-001", ProductName = "Zincir Urun",
            Quantity = 7, UnitPrice = 100m, TotalPrice = 700m, TenantId = product.TenantId
        });

        _orderRepo.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        // ── Z1 EXECUTE: stok 100 → 93 ──
        var z1 = new OrderPlacedStockDeductionHandler(_orderRepo.Object, _productRepo.Object,
            _uow.Object, _lock.Object, _logZ1.Object);
        await z1.HandleAsync(orderId, "ORD-CHAIN-001", CancellationToken.None);

        product.Stock.Should().Be(93, "100 - 7 = 93");

        // ── Z9 SETUP ──
        var mapping = new ProductPlatformMapping
        {
            ProductId = product.Id, TenantId = product.TenantId,
            PlatformType = PlatformType.Trendyol, IsEnabled = true, StoreId = Guid.NewGuid()
        };
        _mappingRepo.Setup(r => r.GetByProductIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping }.AsReadOnly());

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock.Setup(a => a.SupportsStockUpdate).Returns(true);
        adapterMock.Setup(a => a.PushStockUpdateAsync(product.Id, 93, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _factory.Setup(f => f.Resolve(PlatformType.Trendyol)).Returns(adapterMock.Object);

        // ── Z9 EXECUTE: stok 93 → Trendyol push ──
        var z9 = new StockChangedPlatformSyncHandler(_mappingRepo.Object, _factory.Object,
            _syncLogRepo.Object, _uow.Object, _logZ9.Object);
        await z9.HandleAsync(product.Id, product.TenantId, "CHAIN-001",
            100, 93, StockMovementType.Sale, CancellationToken.None);

        // ── ASSERT ──
        adapterMock.Verify(a => a.PushStockUpdateAsync(product.Id, 93, It.IsAny<CancellationToken>()), Times.Once,
            "Trendyol adapter should receive new stock = 93");
        _syncLogRepo.Verify(r => r.AddAsync(It.Is<SyncLog>(s => s.IsSuccess && s.PlatformCode == "Trendyol"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FullChain_ZeroStock_ShouldSyncZeroToAdapter()
    {
        // Sipariş ile stok 0'a düşürme
        var product = new Product
        {
            SKU = "ZERO-001", Name = "Son Stok", MinimumStock = 1,
            CategoryId = Guid.NewGuid(), TenantId = Guid.NewGuid()
        };
        product.SyncStock(3, "seed");

        var orderId = Guid.NewGuid();
        var order = new Order
        {
            OrderNumber = "ORD-ZERO", TenantId = product.TenantId, CustomerId = Guid.NewGuid()
        };
        order.AddItem(new OrderItem
        {
            ProductId = product.Id, ProductSKU = "ZERO-001", ProductName = "Son Stok",
            Quantity = 3, UnitPrice = 50m, TotalPrice = 150m, TenantId = product.TenantId
        });

        _orderRepo.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var z1 = new OrderPlacedStockDeductionHandler(_orderRepo.Object, _productRepo.Object,
            _uow.Object, _lock.Object, _logZ1.Object);
        await z1.HandleAsync(orderId, "ORD-ZERO", CancellationToken.None);

        product.Stock.Should().Be(0);

        // Z9
        var mapping = new ProductPlatformMapping
        {
            ProductId = product.Id, TenantId = product.TenantId,
            PlatformType = PlatformType.Trendyol, IsEnabled = true, StoreId = Guid.NewGuid()
        };
        _mappingRepo.Setup(r => r.GetByProductIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping }.AsReadOnly());

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock.Setup(a => a.SupportsStockUpdate).Returns(true);
        adapterMock.Setup(a => a.PushStockUpdateAsync(product.Id, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _factory.Setup(f => f.Resolve(PlatformType.Trendyol)).Returns(adapterMock.Object);

        var z9 = new StockChangedPlatformSyncHandler(_mappingRepo.Object, _factory.Object,
            _syncLogRepo.Object, _uow.Object, _logZ9.Object);
        await z9.HandleAsync(product.Id, product.TenantId, "ZERO-001",
            3, 0, StockMovementType.Sale, CancellationToken.None);

        adapterMock.Verify(a => a.PushStockUpdateAsync(product.Id, 0, It.IsAny<CancellationToken>()), Times.Once,
            "stock 0 should still be pushed to platform");
    }

    [Fact]
    public async Task FullChain_MultiPlatform_ShouldSyncToAllAdapters()
    {
        var product = new Product
        {
            SKU = "MULTI-001", Name = "Multi Platform", MinimumStock = 5,
            CategoryId = Guid.NewGuid(), TenantId = Guid.NewGuid()
        };
        product.SyncStock(50, "seed");

        var orderId = Guid.NewGuid();
        var order = new Order
        {
            OrderNumber = "ORD-MULTI", TenantId = product.TenantId, CustomerId = Guid.NewGuid()
        };
        order.AddItem(new OrderItem
        {
            ProductId = product.Id, ProductSKU = "MULTI-001", ProductName = "Multi",
            Quantity = 10, UnitPrice = 100m, TotalPrice = 1000m, TenantId = product.TenantId
        });

        _orderRepo.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var z1 = new OrderPlacedStockDeductionHandler(_orderRepo.Object, _productRepo.Object,
            _uow.Object, _lock.Object, _logZ1.Object);
        await z1.HandleAsync(orderId, "ORD-MULTI", CancellationToken.None);
        product.Stock.Should().Be(40);

        // Z9 — 2 platform
        var tyMapping = new ProductPlatformMapping
        {
            ProductId = product.Id, TenantId = product.TenantId,
            PlatformType = PlatformType.Trendyol, IsEnabled = true, StoreId = Guid.NewGuid()
        };
        var hbMapping = new ProductPlatformMapping
        {
            ProductId = product.Id, TenantId = product.TenantId,
            PlatformType = PlatformType.Hepsiburada, IsEnabled = true, StoreId = Guid.NewGuid()
        };
        _mappingRepo.Setup(r => r.GetByProductIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { tyMapping, hbMapping }.AsReadOnly());

        var tyAdapter = new Mock<IIntegratorAdapter>();
        tyAdapter.Setup(a => a.SupportsStockUpdate).Returns(true);
        tyAdapter.Setup(a => a.PushStockUpdateAsync(It.IsAny<Guid>(), 40, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var hbAdapter = new Mock<IIntegratorAdapter>();
        hbAdapter.Setup(a => a.SupportsStockUpdate).Returns(true);
        hbAdapter.Setup(a => a.PushStockUpdateAsync(It.IsAny<Guid>(), 40, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _factory.Setup(f => f.Resolve(PlatformType.Trendyol)).Returns(tyAdapter.Object);
        _factory.Setup(f => f.Resolve(PlatformType.Hepsiburada)).Returns(hbAdapter.Object);

        var z9 = new StockChangedPlatformSyncHandler(_mappingRepo.Object, _factory.Object,
            _syncLogRepo.Object, _uow.Object, _logZ9.Object);
        await z9.HandleAsync(product.Id, product.TenantId, "MULTI-001",
            50, 40, StockMovementType.Sale, CancellationToken.None);

        tyAdapter.Verify(a => a.PushStockUpdateAsync(product.Id, 40, It.IsAny<CancellationToken>()), Times.Once);
        hbAdapter.Verify(a => a.PushStockUpdateAsync(product.Id, 40, It.IsAny<CancellationToken>()), Times.Once);
        _syncLogRepo.Verify(r => r.AddAsync(It.IsAny<SyncLog>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task FullChain_AdapterFails_ShouldStillLogSync()
    {
        var product = new Product
        {
            SKU = "FAIL-001", Name = "Fail Product", MinimumStock = 5,
            CategoryId = Guid.NewGuid(), TenantId = Guid.NewGuid()
        };
        product.SyncStock(20, "seed");

        var mapping = new ProductPlatformMapping
        {
            ProductId = product.Id, TenantId = product.TenantId,
            PlatformType = PlatformType.Trendyol, IsEnabled = true, StoreId = Guid.NewGuid()
        };
        _mappingRepo.Setup(r => r.GetByProductIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping }.AsReadOnly());

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock.Setup(a => a.SupportsStockUpdate).Returns(true);
        adapterMock.Setup(a => a.PushStockUpdateAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Trendyol 429 Too Many Requests"));
        _factory.Setup(f => f.Resolve(PlatformType.Trendyol)).Returns(adapterMock.Object);

        var z9 = new StockChangedPlatformSyncHandler(_mappingRepo.Object, _factory.Object,
            _syncLogRepo.Object, _uow.Object, _logZ9.Object);

        // Should NOT throw
        await z9.HandleAsync(product.Id, product.TenantId, "FAIL-001",
            20, 15, StockMovementType.Sale, CancellationToken.None);

        _syncLogRepo.Verify(r => r.AddAsync(It.Is<SyncLog>(s =>
            !s.IsSuccess && s.ErrorMessage!.Contains("429")), It.IsAny<CancellationToken>()), Times.Once);
    }
}
