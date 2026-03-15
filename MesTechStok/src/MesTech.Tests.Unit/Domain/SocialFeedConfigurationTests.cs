using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// SocialFeedConfiguration entity domain logic tests.
/// Bu testler kirilirsa = sosyal feed yapilandirma mantigi bozulmus demektir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "SocialFeedConfiguration")]
public class SocialFeedConfigurationTests
{
    private static readonly Guid ValidTenantId = Guid.NewGuid();

    // ════ 1. Create_ValidParams_Success ════

    [Fact]
    public void Create_ValidParams_Success()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var platform = SocialFeedPlatform.GoogleMerchant;
        var interval = TimeSpan.FromHours(12);

        // Act
        var config = SocialFeedConfiguration.Create(tenantId, platform, interval);

        // Assert
        config.TenantId.Should().Be(tenantId);
        config.Platform.Should().Be(SocialFeedPlatform.GoogleMerchant);
        config.RefreshInterval.Should().Be(interval);
        config.IsActive.Should().BeTrue();
        config.LastGeneratedAt.Should().BeNull();
        config.ItemCount.Should().Be(0);
        config.LastError.Should().BeNull();
        config.Id.Should().NotBe(Guid.Empty);
    }

    // ════ 2. Create_EmptyTenantId_Throws ════

    [Fact]
    public void Create_EmptyTenantId_Throws()
    {
        // Arrange
        var emptyTenantId = Guid.Empty;

        // Act
        var act = () => SocialFeedConfiguration.Create(emptyTenantId, SocialFeedPlatform.FacebookShop);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tenantId");
    }

    // ════ 3. RecordGeneration_UpdatesLastGeneratedAt ════

    [Fact]
    public void RecordGeneration_UpdatesLastGeneratedAt()
    {
        // Arrange
        var config = SocialFeedConfiguration.Create(ValidTenantId, SocialFeedPlatform.GoogleMerchant);
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        config.RecordGeneration("https://example.com/feed.xml", 50);

        // Assert
        config.LastGeneratedAt.Should().NotBeNull();
        config.LastGeneratedAt!.Value.Should().BeAfter(before);
        config.FeedUrl.Should().Be("https://example.com/feed.xml");
    }

    // ════ 4. RecordGeneration_IncrementsItemCount ════

    [Fact]
    public void RecordGeneration_IncrementsItemCount()
    {
        // Arrange
        var config = SocialFeedConfiguration.Create(ValidTenantId, SocialFeedPlatform.Akakce);

        // Act
        config.RecordGeneration("https://example.com/feed.xml", 120);

        // Assert
        config.ItemCount.Should().Be(120);
    }

    // ════ 5. RecordError_SetsLastError ════

    [Fact]
    public void RecordError_SetsLastError()
    {
        // Arrange
        var config = SocialFeedConfiguration.Create(ValidTenantId, SocialFeedPlatform.Cimri);
        var errorMessage = "Feed uretimi sirasinda XML parse hatasi olustu.";

        // Act
        config.RecordError(errorMessage);

        // Assert
        config.LastError.Should().Be(errorMessage);
        config.LastGeneratedAt.Should().BeNull("RecordError sadece hata kaydeder, uretim zamani degismez");
    }

    // ════ 6. NeedsRefresh_WhenIntervalPassed_ReturnsTrue ════

    [Fact]
    public void NeedsRefresh_WhenIntervalPassed_ReturnsTrue()
    {
        // Arrange
        var config = SocialFeedConfiguration.Create(
            ValidTenantId,
            SocialFeedPlatform.GoogleMerchant,
            refreshInterval: TimeSpan.FromHours(6));

        // Feed was last generated 7 hours ago — interval exceeded
        config.RecordGeneration("https://example.com/feed.xml", 10);
        config.LastGeneratedAt = DateTime.UtcNow.AddHours(-7);

        // Act & Assert
        config.NeedsRefresh.Should().BeTrue();
    }

    // ════ 7. NeedsRefresh_WhenJustGenerated_ReturnsFalse ════

    [Fact]
    public void NeedsRefresh_WhenJustGenerated_ReturnsFalse()
    {
        // Arrange
        var config = SocialFeedConfiguration.Create(
            ValidTenantId,
            SocialFeedPlatform.GoogleMerchant,
            refreshInterval: TimeSpan.FromHours(6));

        // Feed was just generated — interval not exceeded
        config.RecordGeneration("https://example.com/feed.xml", 10);

        // Act & Assert
        config.NeedsRefresh.Should().BeFalse();
    }

    // ════ 8. Deactivate_SetsIsActiveFalse ════

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        // Arrange
        var config = SocialFeedConfiguration.Create(ValidTenantId, SocialFeedPlatform.InstagramShop);
        config.IsActive.Should().BeTrue("yeni olusturulan config varsayilan olarak aktif olmalidir");

        // Act
        config.IsActive = false;

        // Assert
        config.IsActive.Should().BeFalse();
    }
}
