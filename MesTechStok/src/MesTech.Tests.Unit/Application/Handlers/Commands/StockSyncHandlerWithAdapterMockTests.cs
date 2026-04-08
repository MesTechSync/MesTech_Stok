using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using ISyncLogRepository = MesTech.Domain.Interfaces.ISyncLogRepository;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// HH-DEV5-003: StockChangedPlatformSyncHandler test with real adapter mocks.
/// Tests the full sync flow: mapping lookup → adapter resolve → push → sync log.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "StockSync")]
[Trait("Phase", "Dalga15")]
public class StockSyncHandlerWithAdapterMockTests
{
    private readonly Mock<IProductPlatformMappingRepository> _mappingRepo = new();
    private readonly Mock<IAdapterFactory> _adapterFactory = new();
    private readonly Mock<ISyncLogRepository> _syncLogRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<StockChangedPlatformSyncHandler>> _logger = new();

    private StockChangedPlatformSyncHandler CreateSut() =>
        new(_mappingRepo.Object, _adapterFactory.Object, _syncLogRepo.Object, _uow.Object, _logger.Object);

    private static ProductPlatformMapping CreateMapping(Guid productId, PlatformType platform, bool isEnabled = true)
    {
        return new ProductPlatformMapping
        {
            TenantId = Guid.NewGuid(),
            ProductId = productId,
            PlatformType = platform,
            ExternalProductId = "EXT-001",
            IsEnabled = isEnabled,
            SyncStatus = SyncStatus.NotSynced
        };
    }

    // ═══════════════════════════════════════════
    // No Mappings — Skip sync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task HandleAsync_NoMappings_SkipsSyncAndDoesNotCallAdapter()
    {
        var productId = Guid.NewGuid();
        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping>());

        var sut = CreateSut();

        await sut.HandleAsync(productId, Guid.NewGuid(), "SKU-001", 50, 45, StockMovementType.Sale, CancellationToken.None);

        _adapterFactory.Verify(f => f.Resolve(It.IsAny<PlatformType>()), Times.Never);
        _syncLogRepo.Verify(r => r.AddAsync(It.IsAny<SyncLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════
    // All Mappings Disabled — Skip sync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task HandleAsync_AllMappingsDisabled_SkipsSyncEntirely()
    {
        var productId = Guid.NewGuid();
        var mapping = CreateMapping(productId, PlatformType.Trendyol, isEnabled: false);

        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping });

        var sut = CreateSut();

        await sut.HandleAsync(productId, Guid.NewGuid(), "SKU-DIS", 50, 45, StockMovementType.Sale, CancellationToken.None);

        _adapterFactory.Verify(f => f.Resolve(It.IsAny<PlatformType>()), Times.Never);
    }

    // ═══════════════════════════════════════════
    // Successful Sync — Adapter push + SyncLog
    // ═══════════════════════════════════════════

    [Fact]
    public async Task HandleAsync_ActiveMapping_CallsAdapterPushAndSavesSyncLog()
    {
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var mapping = CreateMapping(productId, PlatformType.Trendyol);

        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping });

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.SupportsStockUpdate).Returns(true);
        mockAdapter.Setup(a => a.PushStockUpdateAsync(productId, 45, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _adapterFactory.Setup(f => f.Resolve(PlatformType.Trendyol))
            .Returns(mockAdapter.Object);

        var sut = CreateSut();

        await sut.HandleAsync(productId, tenantId, "SKU-OK", 50, 45, StockMovementType.Sale, CancellationToken.None);

        // Adapter was called with correct stock
        mockAdapter.Verify(a => a.PushStockUpdateAsync(productId, 45, It.IsAny<CancellationToken>()), Times.Once);

        // SyncLog was saved
        _syncLogRepo.Verify(r => r.AddAsync(
            It.Is<SyncLog>(s => s.IsSuccess && s.SyncStatus == SyncStatus.Synced),
            It.IsAny<CancellationToken>()), Times.Once);

        // Mapping updated with sync date
        _mappingRepo.Verify(r => r.UpdateAsync(
            It.Is<ProductPlatformMapping>(m => m.SyncStatus == SyncStatus.Synced),
            It.IsAny<CancellationToken>()), Times.Once);

        // UnitOfWork committed
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════
    // Adapter Returns False — Failure logged
    // ═══════════════════════════════════════════

    [Fact]
    public async Task HandleAsync_AdapterReturnsFalse_LogsFailedSyncLog()
    {
        var productId = Guid.NewGuid();
        var mapping = CreateMapping(productId, PlatformType.OpenCart);

        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping });

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.SupportsStockUpdate).Returns(true);
        mockAdapter.Setup(a => a.PushStockUpdateAsync(productId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _adapterFactory.Setup(f => f.Resolve(PlatformType.OpenCart))
            .Returns(mockAdapter.Object);

        var sut = CreateSut();

        await sut.HandleAsync(productId, Guid.NewGuid(), "SKU-FAIL", 20, 10, StockMovementType.Adjustment, CancellationToken.None);

        _syncLogRepo.Verify(r => r.AddAsync(
            It.Is<SyncLog>(s => !s.IsSuccess && s.SyncStatus == SyncStatus.Failed),
            It.IsAny<CancellationToken>()), Times.Once);

        // Mapping should NOT be updated on failure
        _mappingRepo.Verify(r => r.UpdateAsync(It.IsAny<ProductPlatformMapping>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════
    // Adapter Not Found — Skip platform
    // ═══════════════════════════════════════════

    [Fact]
    public async Task HandleAsync_AdapterNotFound_SkipsPlatformAndDoesNotThrow()
    {
        var productId = Guid.NewGuid();
        var mapping = CreateMapping(productId, PlatformType.N11);

        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping });

        _adapterFactory.Setup(f => f.Resolve(PlatformType.N11))
            .Returns((IIntegratorAdapter?)null);

        var sut = CreateSut();

        var act = () => sut.HandleAsync(productId, Guid.NewGuid(), "SKU-N11", 100, 95, StockMovementType.Sale, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _syncLogRepo.Verify(r => r.AddAsync(It.IsAny<SyncLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════
    // Adapter Does Not Support Stock Update
    // ═══════════════════════════════════════════

    [Fact]
    public async Task HandleAsync_AdapterNoStockSupport_SkipsPlatform()
    {
        var productId = Guid.NewGuid();
        var mapping = CreateMapping(productId, PlatformType.Hepsiburada);

        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping });

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.SupportsStockUpdate).Returns(false);

        _adapterFactory.Setup(f => f.Resolve(PlatformType.Hepsiburada))
            .Returns(mockAdapter.Object);

        var sut = CreateSut();

        await sut.HandleAsync(productId, Guid.NewGuid(), "SKU-HB", 50, 40, StockMovementType.Sale, CancellationToken.None);

        mockAdapter.Verify(a => a.PushStockUpdateAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════
    // Adapter Throws Exception — Error logged in SyncLog
    // ═══════════════════════════════════════════

    [Fact]
    public async Task HandleAsync_AdapterThrowsException_LogsErrorInSyncLog()
    {
        var productId = Guid.NewGuid();
        var mapping = CreateMapping(productId, PlatformType.Trendyol);

        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping });

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.SupportsStockUpdate).Returns(true);
        mockAdapter.Setup(a => a.PushStockUpdateAsync(productId, 30, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Platform API unavailable"));

        _adapterFactory.Setup(f => f.Resolve(PlatformType.Trendyol))
            .Returns(mockAdapter.Object);

        var sut = CreateSut();

        // Should not throw — exception handled internally
        var act = () => sut.HandleAsync(productId, Guid.NewGuid(), "SKU-EX", 50, 30, StockMovementType.Sale, CancellationToken.None);

        await act.Should().NotThrowAsync();

        _syncLogRepo.Verify(r => r.AddAsync(
            It.Is<SyncLog>(s => !s.IsSuccess && s.ErrorMessage!.Contains("unavailable")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════
    // Multiple Platforms — Each synced independently
    // ═══════════════════════════════════════════

    [Fact]
    public async Task HandleAsync_MultipleMappings_SyncsEachPlatformIndependently()
    {
        var productId = Guid.NewGuid();
        var mapping1 = CreateMapping(productId, PlatformType.Trendyol);
        var mapping2 = CreateMapping(productId, PlatformType.OpenCart);

        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping1, mapping2 });

        var trendyolAdapter = new Mock<IIntegratorAdapter>();
        trendyolAdapter.Setup(a => a.SupportsStockUpdate).Returns(true);
        trendyolAdapter.Setup(a => a.PushStockUpdateAsync(productId, 20, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var opencartAdapter = new Mock<IIntegratorAdapter>();
        opencartAdapter.Setup(a => a.SupportsStockUpdate).Returns(true);
        opencartAdapter.Setup(a => a.PushStockUpdateAsync(productId, 20, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _adapterFactory.Setup(f => f.Resolve(PlatformType.Trendyol)).Returns(trendyolAdapter.Object);
        _adapterFactory.Setup(f => f.Resolve(PlatformType.OpenCart)).Returns(opencartAdapter.Object);

        var sut = CreateSut();

        await sut.HandleAsync(productId, Guid.NewGuid(), "SKU-MULTI", 30, 20, StockMovementType.Sale, CancellationToken.None);

        // Both adapters called
        trendyolAdapter.Verify(a => a.PushStockUpdateAsync(productId, 20, It.IsAny<CancellationToken>()), Times.Once);
        opencartAdapter.Verify(a => a.PushStockUpdateAsync(productId, 20, It.IsAny<CancellationToken>()), Times.Once);

        // Two sync logs saved
        _syncLogRepo.Verify(r => r.AddAsync(It.IsAny<SyncLog>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // ═══════════════════════════════════════════
    // Zero Stock Warning
    // ═══════════════════════════════════════════

    [Fact]
    public async Task HandleAsync_ZeroStock_LogsWarningAndSyncs()
    {
        var productId = Guid.NewGuid();
        var mapping = CreateMapping(productId, PlatformType.Trendyol);

        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductPlatformMapping> { mapping });

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.SupportsStockUpdate).Returns(true);
        mockAdapter.Setup(a => a.PushStockUpdateAsync(productId, 0, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _adapterFactory.Setup(f => f.Resolve(PlatformType.Trendyol)).Returns(mockAdapter.Object);

        var sut = CreateSut();

        await sut.HandleAsync(productId, Guid.NewGuid(), "SKU-ZERO", 5, 0, StockMovementType.Sale, CancellationToken.None);

        // Zero stock should still push to platform
        mockAdapter.Verify(a => a.PushStockUpdateAsync(productId, 0, It.IsAny<CancellationToken>()), Times.Once);
    }
}
