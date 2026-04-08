using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Jobs;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure.Jobs;

/// <summary>
/// D12-20 — ProductImportSyncJob unit testleri.
/// 3 aşama: QuickDelta, PoolScan, FullReconciliation.
/// Mock adapter + mock repo ile upsert logic doğrulama.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "D12-Import")]
public class ProductImportSyncJobTests
{
    private readonly Mock<IAdapterFactory> _factory = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public ProductImportSyncJobTests()
    {
        _tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(TenantId);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ProductImportSyncJob CreateSut() => new(
        _factory.Object, _productRepo.Object, _uow.Object,
        _tenantProvider.Object, Mock.Of<ILogger<ProductImportSyncJob>>());

    private static Product MakePlatformProduct(string sku, string? barcode = null, int stock = 50, decimal price = 100m)
        => new()
        {
            SKU = sku, Barcode = barcode, Stock = stock, SalePrice = price,
            Name = $"Product {sku}", CategoryId = Guid.NewGuid()
        };

    // ══════════════════════════════════════
    // QuickDelta
    // ══════════════════════════════════════

    [Fact]
    public async Task QuickDelta_AdapterNotFound_ShouldReturn()
    {
        _factory.Setup(f => f.Resolve("Trendyol")).Returns((IIntegratorAdapter?)null);

        var sut = CreateSut();
        await sut.ExecuteQuickDeltaAsync("Trendyol", CancellationToken.None);

        _productRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task QuickDelta_EmptyResult_ShouldSkip()
    {
        var adapter = new Mock<IIntegratorAdapter>();
        adapter.Setup(a => a.SyncProductsDeltaAsync(It.IsAny<DateTime>(), 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductSyncResult.Empty);
        _factory.Setup(f => f.Resolve("Trendyol")).Returns(adapter.Object);

        var sut = CreateSut();
        await sut.ExecuteQuickDeltaAsync("Trendyol", CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task QuickDelta_NewProduct_ShouldCreate()
    {
        var newProduct = MakePlatformProduct("NEW-001", "8690001000011");
        var syncResult = new ProductSyncResult(
            new List<Product> { newProduct }.AsReadOnly(), 1, 1, 1, TimeSpan.FromSeconds(2), DateTime.UtcNow);

        var adapter = new Mock<IIntegratorAdapter>();
        adapter.Setup(a => a.SyncProductsDeltaAsync(It.IsAny<DateTime>(), 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResult);
        _factory.Setup(f => f.Resolve("Trendyol")).Returns(adapter.Object);

        _productRepo.Setup(r => r.GetByBarcodeAsync("8690001000011", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var sut = CreateSut();
        await sut.ExecuteQuickDeltaAsync("Trendyol", CancellationToken.None);

        _productRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task QuickDelta_ExistingProduct_StockChanged_ShouldUpdate()
    {
        var existing = MakePlatformProduct("EX-001", "8690001000011", stock: 100, price: 200m);
        existing.TenantId = TenantId;

        var incoming = MakePlatformProduct("EX-001", "8690001000011", stock: 85, price: 200m);
        var syncResult = new ProductSyncResult(
            new List<Product> { incoming }.AsReadOnly(), 1, 1, 1, TimeSpan.FromSeconds(1), DateTime.UtcNow);

        var adapter = new Mock<IIntegratorAdapter>();
        adapter.Setup(a => a.SyncProductsDeltaAsync(It.IsAny<DateTime>(), 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResult);
        _factory.Setup(f => f.Resolve("Trendyol")).Returns(adapter.Object);

        _productRepo.Setup(r => r.GetByBarcodeAsync("8690001000011", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sut = CreateSut();
        await sut.ExecuteQuickDeltaAsync("Trendyol", CancellationToken.None);

        existing.Stock.Should().Be(85, "stock should be synced from platform");
        _productRepo.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QuickDelta_ExistingProduct_NoChange_ShouldNotUpdate()
    {
        var existing = MakePlatformProduct("SAME-001", "8690001000011", stock: 50, price: 100m);
        existing.TenantId = TenantId;

        var incoming = MakePlatformProduct("SAME-001", "8690001000011", stock: 50, price: 100m);
        var syncResult = new ProductSyncResult(
            new List<Product> { incoming }.AsReadOnly(), 1, 1, 1, TimeSpan.FromSeconds(1), DateTime.UtcNow);

        var adapter = new Mock<IIntegratorAdapter>();
        adapter.Setup(a => a.SyncProductsDeltaAsync(It.IsAny<DateTime>(), 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResult);
        _factory.Setup(f => f.Resolve("Trendyol")).Returns(adapter.Object);

        _productRepo.Setup(r => r.GetByBarcodeAsync("8690001000011", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sut = CreateSut();
        await sut.ExecuteQuickDeltaAsync("Trendyol", CancellationToken.None);

        _productRepo.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never,
            "no field changed — should not update");
    }

    // ══════════════════════════════════════
    // PoolScan + FullReconciliation
    // ══════════════════════════════════════

    [Fact]
    public async Task PoolScan_AdapterNull_ShouldReturn()
    {
        _factory.Setup(f => f.Resolve("Hepsiburada")).Returns((IIntegratorAdapter?)null);

        var sut = CreateSut();
        await sut.ExecutePoolScanAsync("Hepsiburada", CancellationToken.None);

        _productRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FullReconciliation_AdapterNull_ShouldReturn()
    {
        _factory.Setup(f => f.Resolve("N11")).Returns((IIntegratorAdapter?)null);

        var sut = CreateSut();
        await sut.ExecuteFullReconciliationAsync("N11", CancellationToken.None);

        _productRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ══════════════════════════════════════
    // Barcode vs SKU fallback
    // ══════════════════════════════════════

    [Fact]
    public async Task QuickDelta_NullBarcode_ShouldFallbackToSKU()
    {
        var incoming = MakePlatformProduct("NOBC-001", barcode: null, stock: 10);
        var syncResult = new ProductSyncResult(
            new List<Product> { incoming }.AsReadOnly(), 1, 1, 1, TimeSpan.FromSeconds(1), DateTime.UtcNow);

        var adapter = new Mock<IIntegratorAdapter>();
        adapter.Setup(a => a.SyncProductsDeltaAsync(It.IsAny<DateTime>(), 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResult);
        _factory.Setup(f => f.Resolve("Trendyol")).Returns(adapter.Object);

        _productRepo.Setup(r => r.GetBySKUAsync("NOBC-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var sut = CreateSut();
        await sut.ExecuteQuickDeltaAsync("Trendyol", CancellationToken.None);

        // Barcode null → SKU ile arandı → bulunamadı → yeni oluşturuldu
        _productRepo.Verify(r => r.GetBySKUAsync("NOBC-001", It.IsAny<CancellationToken>()), Times.Once);
        _productRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ══════════════════════════════════════
    // Batch save every 100
    // ══════════════════════════════════════

    [Fact]
    public async Task QuickDelta_MultipleProducts_ShouldBatchSave()
    {
        var products = Enumerable.Range(1, 5)
            .Select(i => MakePlatformProduct($"BATCH-{i:D3}", $"869000{i:D7}", stock: i * 10))
            .ToList();

        var syncResult = new ProductSyncResult(
            products.AsReadOnly(), 5, 1, 1, TimeSpan.FromSeconds(1), DateTime.UtcNow);

        var adapter = new Mock<IIntegratorAdapter>();
        adapter.Setup(a => a.SyncProductsDeltaAsync(It.IsAny<DateTime>(), 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResult);
        _factory.Setup(f => f.Resolve("Trendyol")).Returns(adapter.Object);

        _productRepo.Setup(r => r.GetByBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var sut = CreateSut();
        await sut.ExecuteQuickDeltaAsync("Trendyol", CancellationToken.None);

        _productRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    // ══════════════════════════════════════
    // CancellationToken respect
    // ══════════════════════════════════════

    [Fact]
    public async Task QuickDelta_Cancelled_ShouldStopEarly()
    {
        var products = Enumerable.Range(1, 100)
            .Select(i => MakePlatformProduct($"CAN-{i:D3}", stock: i))
            .ToList();

        var syncResult = new ProductSyncResult(
            products.AsReadOnly(), 100, 1, 1, TimeSpan.FromSeconds(1), DateTime.UtcNow);

        var adapter = new Mock<IIntegratorAdapter>();
        adapter.Setup(a => a.SyncProductsDeltaAsync(It.IsAny<DateTime>(), 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResult);
        _factory.Setup(f => f.Resolve("Trendyol")).Returns(adapter.Object);

        _productRepo.Setup(r => r.GetBySKUAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // hemen iptal

        var sut = CreateSut();
        await sut.ExecuteQuickDeltaAsync("Trendyol", cts.Token);

        // Cancelled → hiç ürün eklenmemeli
        _productRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
