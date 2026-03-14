using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// DEV 5 — Sprint B Task B-05-A: DropshippingPool, DropshippingPoolProduct, FeedImportLog entity testleri.
/// Mock yok — gerçek implementasyonu test et.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "DropshippingPool")]
public class DropshippingPoolTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _poolId   = Guid.NewGuid();
    private static readonly Guid _productId = Guid.NewGuid();
    private static readonly Guid _feedId   = Guid.NewGuid();

    // ─────────────────────────────────────────────────────────────
    // DropshippingPool — Create
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var pool = new DropshippingPool(_tenantId, "Test Havuzu", "Açıklama", isPublic: true, PoolPricingStrategy.Fixed);

        // Assert
        pool.TenantId.Should().Be(_tenantId);
        pool.Name.Should().Be("Test Havuzu");
        pool.Description.Should().Be("Açıklama");
        pool.IsPublic.Should().BeTrue();
        pool.PricingStrategy.Should().Be(PoolPricingStrategy.Fixed);
        pool.IsActive.Should().BeTrue("yeni havuz varsayılan olarak aktif olmalı");
        pool.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithMinimalData_UsesDefaults()
    {
        // Arrange & Act
        var pool = new DropshippingPool(_tenantId, "Minimal Havuz");

        // Assert
        pool.Name.Should().Be("Minimal Havuz");
        pool.IsPublic.Should().BeFalse("varsayılan havuz gizli olmalı");
        pool.PricingStrategy.Should().Be(PoolPricingStrategy.Markup, "varsayılan strateji Markup olmalı");
        pool.Description.Should().BeNull();
    }

    [Fact]
    public void Create_WithNameHavingLeadingTrailingSpaces_TrimsName()
    {
        // Arrange & Act
        var pool = new DropshippingPool(_tenantId, "  Boşluklu Ad  ");

        // Assert
        pool.Name.Should().Be("Boşluklu Ad");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithEmptyOrWhitespaceName_ThrowsArgumentException(string? badName)
    {
        // Arrange & Act
        var act = () => new DropshippingPool(_tenantId, badName!);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Create_WithNullDescription_SetsDescriptionNull()
    {
        // Arrange & Act
        var pool = new DropshippingPool(_tenantId, "Ad", description: null);

        // Assert
        pool.Description.Should().BeNull();
    }

    [Fact]
    public void Create_Products_CollectionIsEmpty()
    {
        // Arrange & Act
        var pool = new DropshippingPool(_tenantId, "Havuz");

        // Assert
        pool.Products.Should().BeEmpty("yeni havuzun ürün listesi boş olmalı");
    }

    // ─────────────────────────────────────────────────────────────
    // DropshippingPool — Activate / Deactivate
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_WhenActive_SetsIsActiveFalse()
    {
        // Arrange
        var pool = new DropshippingPool(_tenantId, "Havuz");
        pool.IsActive.Should().BeTrue();

        // Act
        pool.Deactivate();

        // Assert
        pool.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_WhenInactive_SetsIsActiveTrue()
    {
        // Arrange
        var pool = new DropshippingPool(_tenantId, "Havuz");
        pool.Deactivate();
        pool.IsActive.Should().BeFalse();

        // Act
        pool.Activate();

        // Assert
        pool.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_RemainsActive()
    {
        // Arrange
        var pool = new DropshippingPool(_tenantId, "Havuz");

        // Act
        pool.Activate(); // idempotent

        // Assert
        pool.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_RemainsInactive()
    {
        // Arrange
        var pool = new DropshippingPool(_tenantId, "Havuz");
        pool.Deactivate();

        // Act
        pool.Deactivate(); // idempotent

        // Assert
        pool.IsActive.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────
    // DropshippingPool — Update
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void Update_WithValidData_UpdatesAllProperties()
    {
        // Arrange
        var pool = new DropshippingPool(_tenantId, "Eski Ad", "Eski Açıklama", false, PoolPricingStrategy.Markup);

        // Act
        pool.Update("Yeni Ad", "Yeni Açıklama", isPublic: true, PoolPricingStrategy.Dynamic);

        // Assert
        pool.Name.Should().Be("Yeni Ad");
        pool.Description.Should().Be("Yeni Açıklama");
        pool.IsPublic.Should().BeTrue();
        pool.PricingStrategy.Should().Be(PoolPricingStrategy.Dynamic);
    }

    [Fact]
    public void Update_SetsUpdatedAt()
    {
        // Arrange
        var pool = new DropshippingPool(_tenantId, "Havuz");
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        pool.Update("Yeni Ad", null, false, PoolPricingStrategy.Fixed);

        // Assert
        pool.UpdatedAt.Should().BeAfter(before);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithEmptyName_ThrowsArgumentException(string badName)
    {
        // Arrange
        var pool = new DropshippingPool(_tenantId, "Havuz");

        // Act
        var act = () => pool.Update(badName, null, false, PoolPricingStrategy.Markup);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Update_NameWithSpaces_TrimsName()
    {
        // Arrange
        var pool = new DropshippingPool(_tenantId, "Eski");

        // Act
        pool.Update("  Yeni  ", null, false, PoolPricingStrategy.Markup);

        // Assert
        pool.Name.Should().Be("Yeni");
    }

    // ─────────────────────────────────────────────────────────────
    // PoolPricingStrategy enum
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void PoolPricingStrategy_Markup_HasValueZero()
    {
        ((int)PoolPricingStrategy.Markup).Should().Be(0);
    }

    [Fact]
    public void PoolPricingStrategy_Fixed_HasValueOne()
    {
        ((int)PoolPricingStrategy.Fixed).Should().Be(1);
    }

    [Fact]
    public void PoolPricingStrategy_Dynamic_HasValueTwo()
    {
        ((int)PoolPricingStrategy.Dynamic).Should().Be(2);
    }

    // ─────────────────────────────────────────────────────────────
    // DropshippingPoolProduct — Constructor
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void PoolProduct_Create_WithValidData_SetsProperties()
    {
        // Arrange & Act
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 199.90m, _feedId);

        // Assert
        pp.TenantId.Should().Be(_tenantId);
        pp.PoolId.Should().Be(_poolId);
        pp.ProductId.Should().Be(_productId);
        pp.PoolPrice.Should().Be(199.90m);
        pp.AddedFromFeedId.Should().Be(_feedId);
        pp.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PoolProduct_Create_WithNullFeedId_SetsAddedFromFeedIdNull()
    {
        // Arrange & Act
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 50m, null);

        // Assert
        pp.AddedFromFeedId.Should().BeNull("manuel eklenen ürünlerde feedId null olmalı");
    }

    [Fact]
    public void PoolProduct_Create_WithZeroPrice_IsAllowed()
    {
        // Arrange & Act — sıfır fiyat negatif değil, geçerli
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 0m);

        // Assert
        pp.PoolPrice.Should().Be(0m);
    }

    [Fact]
    public void PoolProduct_Create_WithNegativePrice_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => new DropshippingPoolProduct(_tenantId, _poolId, _productId, -1m);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("poolPrice");
    }

    [Fact]
    public void PoolProduct_Create_WithEmptyPoolId_ThrowsArgumentException()
    {
        // Act
        var act = () => new DropshippingPoolProduct(_tenantId, Guid.Empty, _productId, 100m);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("poolId");
    }

    [Fact]
    public void PoolProduct_Create_WithEmptyProductId_ThrowsArgumentException()
    {
        // Act
        var act = () => new DropshippingPoolProduct(_tenantId, _poolId, Guid.Empty, 100m);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("productId");
    }

    // ─────────────────────────────────────────────────────────────
    // DropshippingPoolProduct — UpdatePrice
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void PoolProduct_UpdatePrice_WithValidPrice_UpdatesPoolPrice()
    {
        // Arrange
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);

        // Act
        pp.UpdatePrice(250m);

        // Assert
        pp.PoolPrice.Should().Be(250m);
    }

    [Fact]
    public void PoolProduct_UpdatePrice_WithZero_IsAllowed()
    {
        // Arrange
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);

        // Act
        pp.UpdatePrice(0m);

        // Assert
        pp.PoolPrice.Should().Be(0m);
    }

    [Fact]
    public void PoolProduct_UpdatePrice_WithNegativePrice_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);

        // Act
        var act = () => pp.UpdatePrice(-0.01m);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("newPrice");
    }

    [Fact]
    public void PoolProduct_UpdatePrice_SetsUpdatedAt()
    {
        // Arrange
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        pp.UpdatePrice(300m);

        // Assert
        pp.UpdatedAt.Should().BeAfter(before);
    }

    // ─────────────────────────────────────────────────────────────
    // DropshippingPoolProduct — Activate / Deactivate
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void PoolProduct_Deactivate_SetsIsActiveFalse()
    {
        // Arrange
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);

        // Act
        pp.Deactivate();

        // Assert
        pp.IsActive.Should().BeFalse();
    }

    [Fact]
    public void PoolProduct_Activate_SetsIsActiveTrue()
    {
        // Arrange
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        pp.Deactivate();

        // Act
        pp.Activate();

        // Assert
        pp.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PoolProduct_Deactivate_SetsUpdatedAt()
    {
        // Arrange
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        pp.Deactivate();

        // Assert
        pp.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void PoolProduct_Activate_SetsUpdatedAt()
    {
        // Arrange
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        pp.Deactivate();
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        pp.Activate();

        // Assert
        pp.UpdatedAt.Should().BeAfter(before);
    }

    // ─────────────────────────────────────────────────────────────
    // FeedImportLog — Constructor
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void FeedImportLog_Create_WithValidFeedId_SetsInProgressStatus()
    {
        // Arrange & Act
        var log = new FeedImportLog(_tenantId, _feedId);

        // Assert
        log.SupplierFeedId.Should().Be(_feedId);
        log.Status.Should().Be(FeedSyncStatus.InProgress);
        log.TenantId.Should().Be(_tenantId);
        log.CompletedAt.Should().BeNull();
        log.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void FeedImportLog_Create_WithEmptyFeedId_ThrowsArgumentException()
    {
        // Act
        var act = () => new FeedImportLog(_tenantId, Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("supplierFeedId");
    }

    [Fact]
    public void FeedImportLog_Create_SetsStartedAtToNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var log = new FeedImportLog(_tenantId, _feedId);

        // Assert
        log.StartedAt.Should().BeAfter(before).And.BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    // ─────────────────────────────────────────────────────────────
    // FeedImportLog — Complete
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void FeedImportLog_Complete_SetsStatusToCompleted()
    {
        // Arrange
        var log = new FeedImportLog(_tenantId, _feedId);

        // Act
        log.Complete(totalProducts: 100, newProducts: 10, updatedProducts: 80, deactivatedProducts: 10);

        // Assert
        log.Status.Should().Be(FeedSyncStatus.Completed);
    }

    [Fact]
    public void FeedImportLog_Complete_SetsCompletedAt()
    {
        // Arrange
        var log = new FeedImportLog(_tenantId, _feedId);
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        log.Complete(100, 10, 80, 10);

        // Assert
        log.CompletedAt.Should().NotBeNull();
        log.CompletedAt!.Value.Should().BeAfter(before);
    }

    [Fact]
    public void FeedImportLog_Complete_SetsProductCounts()
    {
        // Arrange
        var log = new FeedImportLog(_tenantId, _feedId);

        // Act
        log.Complete(totalProducts: 200, newProducts: 30, updatedProducts: 150, deactivatedProducts: 20);

        // Assert
        log.TotalProducts.Should().Be(200);
        log.NewProducts.Should().Be(30);
        log.UpdatedProducts.Should().Be(150);
        log.DeactivatedProducts.Should().Be(20);
    }

    // ─────────────────────────────────────────────────────────────
    // FeedImportLog — Fail
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void FeedImportLog_Fail_SetsStatusToFailed()
    {
        // Arrange
        var log = new FeedImportLog(_tenantId, _feedId);

        // Act
        log.Fail("Bağlantı hatası");

        // Assert
        log.Status.Should().Be(FeedSyncStatus.Failed);
    }

    [Fact]
    public void FeedImportLog_Fail_SetsErrorMessage()
    {
        // Arrange
        var log = new FeedImportLog(_tenantId, _feedId);

        // Act
        log.Fail("HTTP 503 Service Unavailable");

        // Assert
        log.ErrorMessage.Should().Be("HTTP 503 Service Unavailable");
    }

    [Fact]
    public void FeedImportLog_Fail_SetsCompletedAt()
    {
        // Arrange
        var log = new FeedImportLog(_tenantId, _feedId);
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        log.Fail("Hata");

        // Assert
        log.CompletedAt.Should().NotBeNull();
        log.CompletedAt!.Value.Should().BeAfter(before);
    }

    // ─────────────────────────────────────────────────────────────
    // FeedImportLog — ErrorMessage truncation (2000 char limit)
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void FeedImportLog_Fail_ErrorMessage_TruncatedAt2000Chars()
    {
        // Arrange
        var log = new FeedImportLog(_tenantId, _feedId);
        var longError = new string('X', 3000);

        // Act
        log.Fail(longError);

        // Assert
        log.ErrorMessage.Should().HaveLength(2000);
        log.ErrorMessage.Should().Be(new string('X', 2000));
    }

    [Fact]
    public void FeedImportLog_CompletePartially_ErrorMessage_TruncatedAt2000Chars()
    {
        // Arrange
        var log = new FeedImportLog(_tenantId, _feedId);
        var longError = new string('E', 5000);

        // Act
        log.CompletePartially(100, 10, 80, 10, longError);

        // Assert
        log.ErrorMessage.Should().HaveLength(2000);
    }

    [Fact]
    public void FeedImportLog_Fail_ErrorMessageExactly2000Chars_NotTruncated()
    {
        // Arrange
        var log = new FeedImportLog(_tenantId, _feedId);
        var exactError = new string('Y', 2000);

        // Act
        log.Fail(exactError);

        // Assert
        log.ErrorMessage.Should().HaveLength(2000);
    }

    // ─────────────────────────────────────────────────────────────
    // FeedImportLog — Duration
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void FeedImportLog_Duration_WhenNotCompleted_ReturnsNull()
    {
        // Arrange
        var log = new FeedImportLog(_tenantId, _feedId);

        // Assert
        log.Duration.Should().BeNull("tamamlanmamış log için süre null olmalı");
    }

    [Fact]
    public void FeedImportLog_Duration_WhenCompleted_ReturnsPositiveTimeSpan()
    {
        // Arrange
        var log = new FeedImportLog(_tenantId, _feedId);

        // Act
        log.Complete(100, 10, 80, 10);

        // Assert
        log.Duration.Should().NotBeNull();
        log.Duration!.Value.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void FeedImportLog_Duration_WhenFailed_ReturnsPositiveTimeSpan()
    {
        // Arrange
        var log = new FeedImportLog(_tenantId, _feedId);

        // Act
        log.Fail("Hata");

        // Assert
        log.Duration.Should().NotBeNull();
        log.Duration!.Value.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
    }

    // ─────────────────────────────────────────────────────────────
    // FeedImportLog — CompletePartially
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void FeedImportLog_CompletePartially_SetsStatusToPartiallyCompleted()
    {
        // Arrange
        var log = new FeedImportLog(_tenantId, _feedId);

        // Act
        log.CompletePartially(100, 5, 50, 5, "Bazı ürünler işlenemedi");

        // Assert
        log.Status.Should().Be(FeedSyncStatus.PartiallyCompleted);
        log.ErrorMessage.Should().Be("Bazı ürünler işlenemedi");
    }
}
