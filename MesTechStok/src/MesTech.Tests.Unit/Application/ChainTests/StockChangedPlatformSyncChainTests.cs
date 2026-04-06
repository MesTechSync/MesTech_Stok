using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using IProductPlatformMappingRepository = MesTech.Domain.Interfaces.IProductPlatformMappingRepository;
using ISyncLogRepository = MesTech.Domain.Interfaces.ISyncLogRepository;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.ChainTests;

/// <summary>
/// DEV 5 — Zincir 9 E2E: StockChanged → PlatformMapping → Adapter.PushStockUpdateAsync → SyncLog.
/// KÇ-12: Gerçek handler'ı (5 dependency) mock'larla test eder.
/// Önceki test sadece logger'lı eski constructor kullanıyordu.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "ChainE2E")]
[Trait("Platform", "Trendyol")]
public class StockChangedPlatformSyncChainTests
{
    private readonly Mock<IProductPlatformMappingRepository> _mappingRepoMock = new();
    private readonly Mock<IAdapterFactory> _adapterFactoryMock = new();
    private readonly Mock<ISyncLogRepository> _syncLogRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<StockChangedPlatformSyncHandler>> _loggerMock = new();

    private StockChangedPlatformSyncHandler CreateSut() => new(
        _mappingRepoMock.Object,
        _adapterFactoryMock.Object,
        _syncLogRepoMock.Object,
        _uowMock.Object,
        _loggerMock.Object);

    private static ProductPlatformMapping CreateTrendyolMapping(Guid productId, Guid tenantId)
        => new()
        {
            ProductId = productId,
            TenantId = tenantId,
            PlatformType = PlatformType.Trendyol,
            IsEnabled = true,
            ExternalProductId = "TY-EXT-001",
            StoreId = Guid.NewGuid()
        };

    // ══════════════════════════════════════
    // 1. Tam zincir: Stock→Mapping→Adapter→SyncLog
    // ══════════════════════════════════════

    [Fact]
    public async Task HandleAsync_WithActiveMapping_ShouldCallAdapterAndWriteSyncLog()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var mapping = CreateTrendyolMapping(productId, tenantId);

        _mappingRepoMock
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping }.AsReadOnly());

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock.Setup(a => a.SupportsStockUpdate).Returns(true);
        adapterMock
            .Setup(a => a.PushStockUpdateAsync(productId, 45, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _adapterFactoryMock
            .Setup(f => f.Resolve(PlatformType.Trendyol))
            .Returns(adapterMock.Object);

        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(productId, tenantId, "SKU-CHAIN", 50, 45,
            StockMovementType.Sale, CancellationToken.None);

        // Assert — adapter çağrıldı
        adapterMock.Verify(
            a => a.PushStockUpdateAsync(productId, 45, It.IsAny<CancellationToken>()),
            Times.Once,
            "Adapter.PushStockUpdateAsync should be called with new stock quantity");

        // Assert — SyncLog yazıldı
        _syncLogRepoMock.Verify(
            r => r.AddAsync(It.Is<SyncLog>(s =>
                s.PlatformCode == "Trendyol" &&
                s.EntityType == "Product.Stock" &&
                s.IsSuccess == true &&
                s.Direction == SyncDirection.Push &&
                s.ItemsProcessed == 1 &&
                s.ItemsFailed == 0),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "SyncLog should be written with success=true");

        // Assert — Mapping güncellendi
        _mappingRepoMock.Verify(
            r => r.UpdateAsync(It.Is<ProductPlatformMapping>(m =>
                m.SyncStatus == SyncStatus.Synced &&
                m.LastSyncDate != null),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Assert — UoW SaveChanges
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ══════════════════════════════════════
    // 2. Adapter başarısız → SyncLog fail kaydı
    // ══════════════════════════════════════

    [Fact]
    public async Task HandleAsync_AdapterReturnsFalse_ShouldWriteFailedSyncLog()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var mapping = CreateTrendyolMapping(productId, tenantId);

        _mappingRepoMock
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping }.AsReadOnly());

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock.Setup(a => a.SupportsStockUpdate).Returns(true);
        adapterMock
            .Setup(a => a.PushStockUpdateAsync(productId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _adapterFactoryMock
            .Setup(f => f.Resolve(PlatformType.Trendyol))
            .Returns(adapterMock.Object);

        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(productId, tenantId, "SKU-FAIL", 5, 0,
            StockMovementType.Sale, CancellationToken.None);

        // Assert — SyncLog fail olarak yazıldı
        _syncLogRepoMock.Verify(
            r => r.AddAsync(It.Is<SyncLog>(s =>
                s.IsSuccess == false &&
                s.SyncStatus == SyncStatus.Failed &&
                s.ItemsFailed == 1 &&
                s.ErrorMessage != null),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Mapping GÜNCELLENMEMELİ (başarısız)
        _mappingRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<ProductPlatformMapping>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ══════════════════════════════════════
    // 3. Adapter exception → SyncLog exception kaydı
    // ══════════════════════════════════════

    [Fact]
    public async Task HandleAsync_AdapterThrows_ShouldWriteExceptionSyncLog()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var mapping = CreateTrendyolMapping(productId, tenantId);

        _mappingRepoMock
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping }.AsReadOnly());

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock.Setup(a => a.SupportsStockUpdate).Returns(true);
        adapterMock
            .Setup(a => a.PushStockUpdateAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Trendyol API 503 Service Unavailable"));

        _adapterFactoryMock
            .Setup(f => f.Resolve(PlatformType.Trendyol))
            .Returns(adapterMock.Object);

        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act — should NOT throw
        await sut.HandleAsync(productId, tenantId, "SKU-EXC", 10, 5,
            StockMovementType.Sale, CancellationToken.None);

        // Assert — SyncLog exception ile yazıldı
        _syncLogRepoMock.Verify(
            r => r.AddAsync(It.Is<SyncLog>(s =>
                s.IsSuccess == false &&
                s.ErrorMessage!.Contains("503")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════
    // 4. Mapping yok → adapter çağrılmaz
    // ══════════════════════════════════════

    [Fact]
    public async Task HandleAsync_NoMappings_ShouldSkipSync()
    {
        // Arrange
        _mappingRepoMock
            .Setup(r => r.GetByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping>().AsReadOnly());

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "SKU-NONE",
            50, 45, StockMovementType.Sale, CancellationToken.None);

        // Assert — adapter çağrılmadı
        _adapterFactoryMock.Verify(
            f => f.Resolve(It.IsAny<PlatformType>()),
            Times.Never);
        _syncLogRepoMock.Verify(
            r => r.AddAsync(It.IsAny<SyncLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ══════════════════════════════════════
    // 5. Disabled mapping → sync atlanır
    // ══════════════════════════════════════

    [Fact]
    public async Task HandleAsync_DisabledMapping_ShouldSkipSync()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mapping = CreateTrendyolMapping(productId, Guid.NewGuid());
        mapping.IsEnabled = false;

        _mappingRepoMock
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping }.AsReadOnly());

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(productId, Guid.NewGuid(), "SKU-DISABLED",
            50, 45, StockMovementType.Sale, CancellationToken.None);

        // Assert
        _adapterFactoryMock.Verify(
            f => f.Resolve(It.IsAny<PlatformType>()),
            Times.Never);
    }

    // ══════════════════════════════════════
    // 6. Adapter stok güncelleme desteklemiyor → atlanır
    // ══════════════════════════════════════

    [Fact]
    public async Task HandleAsync_AdapterNoStockSupport_ShouldSkipSync()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var mapping = CreateTrendyolMapping(productId, tenantId);

        _mappingRepoMock
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping }.AsReadOnly());

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock.Setup(a => a.SupportsStockUpdate).Returns(false);

        _adapterFactoryMock
            .Setup(f => f.Resolve(PlatformType.Trendyol))
            .Returns(adapterMock.Object);

        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(productId, tenantId, "SKU-NOSUPPORT",
            50, 45, StockMovementType.Sale, CancellationToken.None);

        // Assert — PushStockUpdate çağrılmadı
        adapterMock.Verify(
            a => a.PushStockUpdateAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ══════════════════════════════════════
    // 7. Çoklu platform → her birine sync
    // ══════════════════════════════════════

    [Fact]
    public async Task HandleAsync_MultiplePlatforms_ShouldSyncToAll()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var trendyolMapping = new ProductPlatformMapping
        {
            ProductId = productId, TenantId = tenantId,
            PlatformType = PlatformType.Trendyol, IsEnabled = true,
            StoreId = Guid.NewGuid()
        };
        var n11Mapping = new ProductPlatformMapping
        {
            ProductId = productId, TenantId = tenantId,
            PlatformType = PlatformType.N11, IsEnabled = true,
            StoreId = Guid.NewGuid()
        };

        _mappingRepoMock
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { trendyolMapping, n11Mapping }.AsReadOnly());

        var trendyolAdapter = new Mock<IIntegratorAdapter>();
        trendyolAdapter.Setup(a => a.SupportsStockUpdate).Returns(true);
        trendyolAdapter.Setup(a => a.PushStockUpdateAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var n11Adapter = new Mock<IIntegratorAdapter>();
        n11Adapter.Setup(a => a.SupportsStockUpdate).Returns(true);
        n11Adapter.Setup(a => a.PushStockUpdateAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _adapterFactoryMock.Setup(f => f.Resolve(PlatformType.Trendyol)).Returns(trendyolAdapter.Object);
        _adapterFactoryMock.Setup(f => f.Resolve(PlatformType.N11)).Returns(n11Adapter.Object);

        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(productId, tenantId, "SKU-MULTI",
            50, 30, StockMovementType.Sale, CancellationToken.None);

        // Assert — her iki adapter da çağrıldı
        trendyolAdapter.Verify(a => a.PushStockUpdateAsync(productId, 30, It.IsAny<CancellationToken>()), Times.Once);
        n11Adapter.Verify(a => a.PushStockUpdateAsync(productId, 30, It.IsAny<CancellationToken>()), Times.Once);

        // 2 SyncLog kaydı
        _syncLogRepoMock.Verify(
            r => r.AddAsync(It.IsAny<SyncLog>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    // ══════════════════════════════════════
    // 8. Stok sıfır → uyarı logu
    // ══════════════════════════════════════

    [Fact]
    public async Task HandleAsync_ZeroStock_ShouldLogWarning()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var mapping = CreateTrendyolMapping(productId, tenantId);

        _mappingRepoMock
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping }.AsReadOnly());

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock.Setup(a => a.SupportsStockUpdate).Returns(true);
        adapterMock.Setup(a => a.PushStockUpdateAsync(It.IsAny<Guid>(), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _adapterFactoryMock.Setup(f => f.Resolve(PlatformType.Trendyol)).Returns(adapterMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(productId, tenantId, "SKU-ZERO",
            5, 0, StockMovementType.Sale, CancellationToken.None);

        // Assert — STOK SIFIR uyarısı
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("STOK SIFIR")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Adapter yine de çağrılmalı (stok 0'a çekilecek)
        adapterMock.Verify(
            a => a.PushStockUpdateAsync(productId, 0, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
