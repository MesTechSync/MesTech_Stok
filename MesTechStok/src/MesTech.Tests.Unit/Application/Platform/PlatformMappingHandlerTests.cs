using FluentAssertions;
using MesTech.Application.Commands.MapProductToPlatform;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Application.DTOs;
using MesTech.Domain.Common;
using MesTech.Tests.Unit._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using ISyncLogRepository = MesTech.Domain.Interfaces.ISyncLogRepository;
using IProductPlatformMappingRepository = MesTech.Domain.Interfaces.IProductPlatformMappingRepository;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;

namespace MesTech.Tests.Unit.Application.Platform;

// ════════════════════════════════════════════════════════
// D5-112: Platform Mapping Handler + Domain Tests
// ════════════════════════════════════════════════════════

#region MapProductToPlatformHandler — Extended Tests

[Trait("Category", "Unit")]
[Trait("Feature", "PlatformMapping")]
public class PlatformMappingCreateTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private MapProductToPlatformHandler CreateSut()
    {
        _tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(_tenantId);
        return new(_productRepo.Object, _uow.Object, _tenantProvider.Object);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = () => sut.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldThrowKeyNotFoundException()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var cmd = new MapProductToPlatformCommand(Guid.NewGuid(), PlatformType.OpenCart, "OC-CAT-10");
        var act = () => CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidProduct_ShouldSetTenantIdFromProvider()
    {
        var product = FakeData.CreateProduct();
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        ProductPlatformMapping? captured = null;
        _productRepo.Setup(r => r.AddPlatformMappingAsync(It.IsAny<ProductPlatformMapping>(), It.IsAny<CancellationToken>()))
            .Callback<ProductPlatformMapping, CancellationToken>((m, _) => captured = m);

        await CreateSut().Handle(
            new MapProductToPlatformCommand(product.Id, PlatformType.Trendyol, "TR-CAT-5"), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(_tenantId);
    }

    [Theory]
    [InlineData(PlatformType.Trendyol, "TR-CAT-1")]
    [InlineData(PlatformType.Hepsiburada, "HB-CAT-99")]
    [InlineData(PlatformType.Amazon, "AMZ-CAT-42")]
    [InlineData(PlatformType.N11, "N11-CAT-7")]
    [InlineData(PlatformType.Ciceksepeti, "CS-CAT-3")]
    public async Task Handle_MultiplePlatforms_ShouldSetCorrectPlatformType(
        PlatformType platform, string categoryId)
    {
        var product = FakeData.CreateProduct();
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        ProductPlatformMapping? captured = null;
        _productRepo.Setup(r => r.AddPlatformMappingAsync(It.IsAny<ProductPlatformMapping>(), It.IsAny<CancellationToken>()))
            .Callback<ProductPlatformMapping, CancellationToken>((m, _) => captured = m);

        await CreateSut().Handle(
            new MapProductToPlatformCommand(product.Id, platform, categoryId), CancellationToken.None);

        captured!.PlatformType.Should().Be(platform);
        captured.ExternalCategoryId.Should().Be(categoryId);
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldCallAddAndSave()
    {
        var product = FakeData.CreateProduct();
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var cmd = new MapProductToPlatformCommand(product.Id, PlatformType.Shopify, "SHOP-CAT-1");
        await CreateSut().Handle(cmd, CancellationToken.None);

        _productRepo.Verify(
            r => r.AddPlatformMappingAsync(It.IsAny<ProductPlatformMapping>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NewMapping_ShouldDefaultToNotSyncedAndEnabled()
    {
        var product = FakeData.CreateProduct();
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        ProductPlatformMapping? captured = null;
        _productRepo.Setup(r => r.AddPlatformMappingAsync(It.IsAny<ProductPlatformMapping>(), It.IsAny<CancellationToken>()))
            .Callback<ProductPlatformMapping, CancellationToken>((m, _) => captured = m);

        await CreateSut().Handle(
            new MapProductToPlatformCommand(product.Id, PlatformType.eBay, "EBAY-CAT-1"), CancellationToken.None);

        captured!.SyncStatus.Should().Be(SyncStatus.NotSynced);
        captured.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_CancellationRequested_ShouldPropagateToken()
    {
        var product = FakeData.CreateProduct();
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _productRepo.Setup(r => r.GetByIdAsync(product.Id, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        var cmd = new MapProductToPlatformCommand(product.Id, PlatformType.WooCommerce, "WC-CAT-1");
        var act = () => CreateSut().Handle(cmd, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

#endregion

#region MapProductToPlatformValidator Tests

[Trait("Category", "Unit")]
[Trait("Feature", "PlatformMapping")]
public class PlatformMappingValidatorTests
{
    private readonly MapProductToPlatformValidator _validator = new();

    [Fact]
    public void Validate_EmptyProductId_ShouldFail()
    {
        var cmd = new MapProductToPlatformCommand(Guid.Empty, PlatformType.Trendyol, "CAT-1");
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public void Validate_EmptyCategoryId_ShouldFail()
    {
        var cmd = new MapProductToPlatformCommand(Guid.NewGuid(), PlatformType.Trendyol, "");
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCategoryId");
    }

    [Fact]
    public void Validate_CategoryIdExceedsMaxLength_ShouldFail()
    {
        var longCat = new string('X', 501);
        var cmd = new MapProductToPlatformCommand(Guid.NewGuid(), PlatformType.Trendyol, longCat);
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCategoryId");
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        var cmd = new MapProductToPlatformCommand(Guid.NewGuid(), PlatformType.Hepsiburada, "HB-CAT-123");
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_CategoryIdAtMaxLength_ShouldPass()
    {
        var maxCat = new string('A', 500);
        var cmd = new MapProductToPlatformCommand(Guid.NewGuid(), PlatformType.Amazon, maxCat);
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region ProductPlatformMapping Domain — Extended Tests

[Trait("Category", "Unit")]
[Trait("Feature", "PlatformMapping")]
public class PlatformMappingDomainTests
{
    [Fact]
    public void NewMapping_ShouldHaveGeneratedId()
    {
        var mapping = new ProductPlatformMapping();
        mapping.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void NewMapping_ShouldHaveCreatedAtSet()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var mapping = new ProductPlatformMapping();
        var after = DateTime.UtcNow.AddSeconds(1);

        mapping.CreatedAt.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void NewMapping_ShouldDefaultLastSyncDateToNull()
    {
        var mapping = new ProductPlatformMapping();
        mapping.LastSyncDate.Should().BeNull();
    }

    [Fact]
    public void NewMapping_ShouldDefaultExternalFieldsToNull()
    {
        var mapping = new ProductPlatformMapping();
        mapping.ExternalProductId.Should().BeNull();
        mapping.ExternalCategoryId.Should().BeNull();
        mapping.ExternalUrl.Should().BeNull();
    }

    [Fact]
    public void Mapping_ShouldAcceptProductVariantId()
    {
        var variantId = Guid.NewGuid();
        var mapping = new ProductPlatformMapping { ProductVariantId = variantId };
        mapping.ProductVariantId.Should().Be(variantId);
    }

    [Fact]
    public void Mapping_ShouldAcceptNullProductVariantId()
    {
        var mapping = new ProductPlatformMapping { ProductVariantId = null };
        mapping.ProductVariantId.Should().BeNull();
    }

    [Fact]
    public void SyncStatus_ShouldTransitionFromNotSyncedToSynced()
    {
        var mapping = new ProductPlatformMapping();
        mapping.SyncStatus.Should().Be(SyncStatus.NotSynced);

        mapping.SyncStatus = SyncStatus.Syncing;
        mapping.SyncStatus.Should().Be(SyncStatus.Syncing);

        mapping.SyncStatus = SyncStatus.Synced;
        mapping.SyncStatus.Should().Be(SyncStatus.Synced);
    }

    [Fact]
    public void SyncStatus_ShouldTransitionToFailed()
    {
        var mapping = new ProductPlatformMapping { SyncStatus = SyncStatus.Syncing };
        mapping.SyncStatus = SyncStatus.Failed;
        mapping.SyncStatus.Should().Be(SyncStatus.Failed);
    }

    [Fact]
    public void SyncStatus_ShouldTransitionToPendingSync()
    {
        var mapping = new ProductPlatformMapping { SyncStatus = SyncStatus.Synced };
        mapping.SyncStatus = SyncStatus.PendingSync;
        mapping.SyncStatus.Should().Be(SyncStatus.PendingSync);
    }

    [Fact]
    public void DisableMapping_ShouldSetIsEnabledFalse()
    {
        var mapping = new ProductPlatformMapping();
        mapping.IsEnabled.Should().BeTrue();

        mapping.IsEnabled = false;
        mapping.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void ReenableMapping_ShouldSetIsEnabledTrue()
    {
        var mapping = new ProductPlatformMapping { IsEnabled = false };
        mapping.IsEnabled.Should().BeFalse();

        mapping.IsEnabled = true;
        mapping.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Mapping_ShouldSupportSoftDelete()
    {
        var mapping = new ProductPlatformMapping();
        mapping.IsDeleted.Should().BeFalse();

        mapping.IsDeleted = true;
        mapping.DeletedAt = DateTime.UtcNow;
        mapping.DeletedBy = "admin";

        mapping.IsDeleted.Should().BeTrue();
        mapping.DeletedAt.Should().NotBeNull();
        mapping.DeletedBy.Should().Be("admin");
    }

    [Fact]
    public void Mapping_ShouldImplementITenantEntity()
    {
        var mapping = new ProductPlatformMapping();
        mapping.Should().BeAssignableTo<ITenantEntity>();
    }

    [Fact]
    public void LastSyncDate_ShouldTrackLatestSync()
    {
        var mapping = new ProductPlatformMapping();
        var syncTime = DateTime.UtcNow;

        mapping.LastSyncDate = syncTime;
        mapping.SyncStatus = SyncStatus.Synced;

        mapping.LastSyncDate.Should().Be(syncTime);
    }

    [Theory]
    [InlineData(PlatformType.OpenCart)]
    [InlineData(PlatformType.Trendyol)]
    [InlineData(PlatformType.N11)]
    [InlineData(PlatformType.Hepsiburada)]
    [InlineData(PlatformType.Amazon)]
    [InlineData(PlatformType.Ciceksepeti)]
    [InlineData(PlatformType.eBay)]
    [InlineData(PlatformType.Shopify)]
    [InlineData(PlatformType.WooCommerce)]
    public void Mapping_ShouldAcceptAllPlatformTypes(PlatformType platform)
    {
        var mapping = new ProductPlatformMapping { PlatformType = platform };
        mapping.PlatformType.Should().Be(platform);
    }

    [Fact]
    public void PlatformSpecificData_ShouldStoreComplexJson()
    {
        var json = "{\"DeliveryType\":\"Cargo\",\"StockCode\":\"CS-001\",\"CommissionRate\":0.15}";
        var mapping = new ProductPlatformMapping
        {
            PlatformType = PlatformType.Ciceksepeti,
            PlatformSpecificData = json
        };

        mapping.PlatformSpecificData.Should().Contain("DeliveryType");
        mapping.PlatformSpecificData.Should().Contain("CommissionRate");
    }

    [Fact]
    public void TwoMappings_SamePlatformDifferentProducts_ShouldHaveDifferentIds()
    {
        var mapping1 = new ProductPlatformMapping
        {
            ProductId = Guid.NewGuid(),
            PlatformType = PlatformType.Trendyol,
            StoreId = Guid.NewGuid()
        };

        var mapping2 = new ProductPlatformMapping
        {
            ProductId = Guid.NewGuid(),
            PlatformType = PlatformType.Trendyol,
            StoreId = mapping1.StoreId
        };

        mapping1.Id.Should().NotBe(mapping2.Id);
    }

    [Fact]
    public void ExternalProductId_ShouldStoreAnyPlatformFormat()
    {
        var mapping = new ProductPlatformMapping
        {
            ExternalProductId = "TR-1234567890",
            ExternalUrl = "https://www.trendyol.com/product/1234567890"
        };

        mapping.ExternalProductId.Should().Be("TR-1234567890");
        mapping.ExternalUrl.Should().StartWith("https://");
    }

    [Fact]
    public void Equality_SameId_ShouldBeEqual()
    {
        var mapping1 = new ProductPlatformMapping();
        var mapping2 = new ProductPlatformMapping();

        // Different IDs — not equal
        mapping1.Should().NotBe(mapping2);

        // Same reference — equal
        mapping1.Should().Be(mapping1);
    }
}

#endregion

#region StockChangedPlatformSyncHandler — Mapping-Focused Tests

[Trait("Category", "Unit")]
[Trait("Feature", "PlatformMapping")]
public class StockSyncPlatformMappingTests
{
    private readonly Mock<IProductPlatformMappingRepository> _mappingRepo = new();
    private readonly Mock<IAdapterFactory> _adapterFactory = new();
    private readonly Mock<ISyncLogRepository> _syncLogRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<StockChangedPlatformSyncHandler>> _logger = new();

    private StockChangedPlatformSyncHandler CreateSut() => new(
        _mappingRepo.Object, _adapterFactory.Object,
        _syncLogRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_NoMappings_ShouldSkipSync()
    {
        var productId = Guid.NewGuid();
        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProductPlatformMapping>());

        await CreateSut().HandleAsync(
            productId, Guid.NewGuid(), "SKU-NONE", 10, 5,
            StockMovementType.Sale, CancellationToken.None);

        _adapterFactory.Verify(f => f.Resolve(It.IsAny<PlatformType>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AllMappingsDisabled_ShouldSkipSync()
    {
        var productId = Guid.NewGuid();
        var disabledMappings = new[]
        {
            new ProductPlatformMapping { ProductId = productId, PlatformType = PlatformType.Trendyol, IsEnabled = false },
            new ProductPlatformMapping { ProductId = productId, PlatformType = PlatformType.Hepsiburada, IsEnabled = false }
        };

        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(disabledMappings);

        await CreateSut().HandleAsync(
            productId, Guid.NewGuid(), "SKU-DIS", 10, 5,
            StockMovementType.Sale, CancellationToken.None);

        _adapterFactory.Verify(f => f.Resolve(It.IsAny<PlatformType>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_MixedEnabledDisabled_ShouldOnlySyncEnabled()
    {
        var productId = Guid.NewGuid();
        var mappings = new[]
        {
            new ProductPlatformMapping { ProductId = productId, PlatformType = PlatformType.Trendyol, IsEnabled = true },
            new ProductPlatformMapping { ProductId = productId, PlatformType = PlatformType.Hepsiburada, IsEnabled = false },
            new ProductPlatformMapping { ProductId = productId, PlatformType = PlatformType.N11, IsEnabled = true }
        };

        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mappings);

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.SupportsStockUpdate).Returns(true);
        mockAdapter.Setup(a => a.PushStockUpdateAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _adapterFactory.Setup(f => f.Resolve(It.IsAny<PlatformType>())).Returns(mockAdapter.Object);

        await CreateSut().HandleAsync(
            productId, Guid.NewGuid(), "SKU-MIX", 10, 5,
            StockMovementType.Sale, CancellationToken.None);

        // Only Trendyol + N11 should be resolved (not Hepsiburada)
        _adapterFactory.Verify(f => f.Resolve(PlatformType.Trendyol), Times.Once);
        _adapterFactory.Verify(f => f.Resolve(PlatformType.N11), Times.Once);
        _adapterFactory.Verify(f => f.Resolve(PlatformType.Hepsiburada), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulSync_ShouldUpdateMappingSyncStatus()
    {
        var productId = Guid.NewGuid();
        var mapping = new ProductPlatformMapping
        {
            ProductId = productId,
            PlatformType = PlatformType.Trendyol,
            IsEnabled = true,
            SyncStatus = SyncStatus.NotSynced
        };

        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { mapping });

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.SupportsStockUpdate).Returns(true);
        mockAdapter.Setup(a => a.PushStockUpdateAsync(productId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _adapterFactory.Setup(f => f.Resolve(PlatformType.Trendyol)).Returns(mockAdapter.Object);

        await CreateSut().HandleAsync(
            productId, Guid.NewGuid(), "SKU-SYNC", 5, 0,
            StockMovementType.Sale, CancellationToken.None);

        mapping.SyncStatus.Should().Be(SyncStatus.Synced);
        mapping.LastSyncDate.Should().NotBeNull();
        _mappingRepo.Verify(r => r.UpdateAsync(mapping, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AdapterDoesNotSupportStockUpdate_ShouldSkip()
    {
        var productId = Guid.NewGuid();
        var mapping = new ProductPlatformMapping
        {
            ProductId = productId,
            PlatformType = PlatformType.Etsy,
            IsEnabled = true
        };

        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { mapping });

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.SupportsStockUpdate).Returns(false);

        _adapterFactory.Setup(f => f.Resolve(PlatformType.Etsy)).Returns(mockAdapter.Object);

        await CreateSut().HandleAsync(
            productId, Guid.NewGuid(), "SKU-NOSTOCK", 10, 5,
            StockMovementType.Sale, CancellationToken.None);

        mockAdapter.Verify(
            a => a.PushStockUpdateAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AdapterThrows_ShouldRecordFailedSyncLogNotThrow()
    {
        var productId = Guid.NewGuid();
        var mapping = new ProductPlatformMapping
        {
            ProductId = productId,
            PlatformType = PlatformType.Amazon,
            IsEnabled = true
        };

        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { mapping });

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.SupportsStockUpdate).Returns(true);
        mockAdapter.Setup(a => a.PushStockUpdateAsync(productId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Timeout"));

        _adapterFactory.Setup(f => f.Resolve(PlatformType.Amazon)).Returns(mockAdapter.Object);

        var act = async () => await CreateSut().HandleAsync(
            productId, Guid.NewGuid(), "SKU-TIMEOUT", 10, 8,
            StockMovementType.Adjustment, CancellationToken.None);

        await act.Should().NotThrowAsync();

        _syncLogRepo.Verify(r => r.AddAsync(
            It.Is<SyncLog>(s => !s.IsSuccess && s.ErrorMessage == "Timeout"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroStock_ShouldStillSync()
    {
        var productId = Guid.NewGuid();
        var mapping = new ProductPlatformMapping
        {
            ProductId = productId,
            PlatformType = PlatformType.Trendyol,
            IsEnabled = true
        };

        _mappingRepo.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { mapping });

        var mockAdapter = new Mock<IIntegratorAdapter>();
        mockAdapter.Setup(a => a.SupportsStockUpdate).Returns(true);
        mockAdapter.Setup(a => a.PushStockUpdateAsync(productId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _adapterFactory.Setup(f => f.Resolve(PlatformType.Trendyol)).Returns(mockAdapter.Object);

        await CreateSut().HandleAsync(
            productId, Guid.NewGuid(), "SKU-ZERO", 5, 0,
            StockMovementType.Sale, CancellationToken.None);

        mockAdapter.Verify(
            a => a.PushStockUpdateAsync(productId, 0, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

#endregion

#region PlatformMappingDto Tests

[Trait("Category", "Unit")]
[Trait("Feature", "PlatformMapping")]
public class PlatformMappingDtoTests
{
    [Fact]
    public void Dto_ShouldDefaultPlatformCodeToEmpty()
    {
        var dto = new PlatformMappingDto();
        dto.PlatformCode.Should().BeEmpty();
        dto.PlatformName.Should().BeEmpty();
    }

    [Fact]
    public void Dto_ShouldMapAllProperties()
    {
        var dto = new PlatformMappingDto
        {
            PlatformCode = "Trendyol",
            PlatformName = "Trendyol Marketplace",
            ExternalProductId = "TR-12345",
            ExternalCategoryId = "CAT-99",
            LastSyncDate = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc),
            IsEnabled = true
        };

        dto.PlatformCode.Should().Be("Trendyol");
        dto.ExternalProductId.Should().Be("TR-12345");
        dto.LastSyncDate.Should().HaveYear(2026);
        dto.IsEnabled.Should().BeTrue();
    }
}

#endregion
