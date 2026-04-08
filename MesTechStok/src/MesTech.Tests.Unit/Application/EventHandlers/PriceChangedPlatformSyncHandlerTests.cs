using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using ISyncLogRepository = MesTech.Domain.Interfaces.ISyncLogRepository;
using IProductPlatformMappingRepository = MesTech.Domain.Interfaces.IProductPlatformMappingRepository;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

[Trait("Category", "Unit")]
[Trait("Layer", "EventHandler")]
public class PriceChangedPlatformSyncHandlerTests
{
    private readonly Mock<IProductPlatformMappingRepository> _mappingRepo = new();
    private readonly Mock<IAdapterFactory> _adapterFactory = new();
    private readonly Mock<ISyncLogRepository> _syncLogRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<PriceChangedPlatformSyncHandler>> _logger = new();

    private PriceChangedPlatformSyncHandler CreateSut() => new(
        _mappingRepo.Object,
        _adapterFactory.Object,
        _syncLogRepo.Object,
        _uow.Object,
        _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldNotThrow()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mappingRepo
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProductPlatformMapping>());

        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(
            productId, Guid.NewGuid(), "SKU-PRICE-01",
            99.90m, 79.90m, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_NoActiveMappings_ShouldReturnCompletedTask()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mappingRepo
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProductPlatformMapping>());

        var sut = CreateSut();

        // Act
        var task = sut.HandleAsync(
            productId, Guid.NewGuid(), "SKU-FAST",
            100m, 80m, CancellationToken.None);

        await task;

        // Assert
        task.IsCompletedSuccessfully.Should().BeTrue();
        _adapterFactory.Verify(f => f.Resolve(It.IsAny<PlatformType>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AdapterNotFound_ShouldLogWarningAndSkip()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mapping = new ProductPlatformMapping
        {
            ProductId = productId,
            PlatformType = PlatformType.Trendyol,
            IsEnabled = true
        };

        _mappingRepo
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { mapping });

        _adapterFactory
            .Setup(f => f.Resolve(PlatformType.Trendyol))
            .Returns((IIntegratorAdapter?)null);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(
            productId, Guid.NewGuid(), "SKU-NOADAPTER",
            50m, 40m, CancellationToken.None);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("adapter bulunamadı")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _syncLogRepo.Verify(r => r.AddAsync(It.IsAny<SyncLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AdapterDoesNotSupportPriceUpdate_ShouldSkip()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mapping = new ProductPlatformMapping
        {
            ProductId = productId,
            PlatformType = PlatformType.Trendyol,
            IsEnabled = true
        };

        _mappingRepo
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { mapping });

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.SupportsPriceUpdate).Returns(false);

        _adapterFactory
            .Setup(f => f.Resolve(PlatformType.Trendyol))
            .Returns(mockAdapter.Object);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(
            productId, Guid.NewGuid(), "SKU-NOSUPPORT",
            50m, 40m, CancellationToken.None);

        // Assert
        mockAdapter.Verify(a => a.PushPriceUpdateAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulSync_ShouldUpdateMappingAndAddSyncLog()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var mapping = new ProductPlatformMapping
        {
            ProductId = productId,
            TenantId = tenantId,
            PlatformType = PlatformType.Trendyol,
            IsEnabled = true,
            SyncStatus = SyncStatus.NotSynced
        };

        _mappingRepo
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { mapping });

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.SupportsPriceUpdate).Returns(true);
        mockAdapter
            .Setup(a => a.PushPriceUpdateAsync(productId, 79.90m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _adapterFactory
            .Setup(f => f.Resolve(PlatformType.Trendyol))
            .Returns(mockAdapter.Object);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(
            productId, tenantId, "SKU-OK",
            99.90m, 79.90m, CancellationToken.None);

        // Assert
        mapping.SyncStatus.Should().Be(SyncStatus.Synced);
        mapping.LastSyncDate.Should().NotBeNull();

        _mappingRepo.Verify(r => r.UpdateAsync(mapping, It.IsAny<CancellationToken>()), Times.Once);
        _syncLogRepo.Verify(r => r.AddAsync(
            It.Is<SyncLog>(s => s.IsSuccess && s.SyncStatus == SyncStatus.Synced && s.ItemsProcessed == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AdapterReturnsFalse_ShouldLogWarningAndRecordFailedSyncLog()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mapping = new ProductPlatformMapping
        {
            ProductId = productId,
            PlatformType = PlatformType.Trendyol,
            IsEnabled = true
        };

        _mappingRepo
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { mapping });

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.SupportsPriceUpdate).Returns(true);
        mockAdapter
            .Setup(a => a.PushPriceUpdateAsync(productId, It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _adapterFactory
            .Setup(f => f.Resolve(PlatformType.Trendyol))
            .Returns(mockAdapter.Object);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(
            productId, Guid.NewGuid(), "SKU-FAIL",
            50m, 40m, CancellationToken.None);

        // Assert
        _syncLogRepo.Verify(r => r.AddAsync(
            It.Is<SyncLog>(s => !s.IsSuccess && s.SyncStatus == SyncStatus.Failed && s.ItemsFailed == 1),
            It.IsAny<CancellationToken>()), Times.Once);

        _mappingRepo.Verify(r => r.UpdateAsync(It.IsAny<ProductPlatformMapping>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AdapterThrowsException_ShouldCatchAndRecordFailedSyncLog()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mapping = new ProductPlatformMapping
        {
            ProductId = productId,
            PlatformType = PlatformType.Trendyol,
            IsEnabled = true
        };

        _mappingRepo
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { mapping });

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.SupportsPriceUpdate).Returns(true);
        mockAdapter
            .Setup(a => a.PushPriceUpdateAsync(productId, It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        _adapterFactory
            .Setup(f => f.Resolve(PlatformType.Trendyol))
            .Returns(mockAdapter.Object);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.HandleAsync(
            productId, Guid.NewGuid(), "SKU-EX",
            50m, 40m, CancellationToken.None);

        // Assert — should not throw (caught internally)
        await act.Should().NotThrowAsync();

        _syncLogRepo.Verify(r => r.AddAsync(
            It.Is<SyncLog>(s => !s.IsSuccess && s.ErrorMessage == "Connection refused"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DisabledMapping_ShouldBeSkipped()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var disabledMapping = new ProductPlatformMapping
        {
            ProductId = productId,
            PlatformType = PlatformType.Trendyol,
            IsEnabled = false
        };

        _mappingRepo
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { disabledMapping });

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(
            productId, Guid.NewGuid(), "SKU-DISABLED",
            50m, 40m, CancellationToken.None);

        // Assert
        _adapterFactory.Verify(f => f.Resolve(It.IsAny<PlatformType>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
