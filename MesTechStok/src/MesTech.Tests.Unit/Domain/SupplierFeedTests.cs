using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using Xunit;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// SupplierFeed entity unit tests — markup calculation, sync recording, defaults.
/// 5 tests: defaults, percent markup, fixed markup, sync success, sync with error.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "SupplierFeed")]
public class SupplierFeedTests
{
    private static SupplierFeed CreateFeed(bool usePercent = true, decimal percentMarkup = 20m, decimal fixedMarkup = 50m)
    {
        return new SupplierFeed
        {
            TenantId = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            Name = "Test Tedarikci Feed",
            FeedUrl = "https://supplier.example.com/feed.xml",
            Format = FeedFormat.Xml,
            UsePercentMarkup = usePercent,
            PriceMarkupPercent = percentMarkup,
            PriceMarkupFixed = fixedMarkup
        };
    }

    // ════ 1. Defaults — new feed has correct defaults ════

    [Fact]
    public void NewFeed_HasCorrectDefaults()
    {
        // Act
        var feed = CreateFeed();

        // Assert
        feed.IsActive.Should().BeTrue();
        feed.AutoDeactivateOnZeroStock.Should().BeTrue();
        feed.AutoActivateOnRestock.Should().BeTrue();
        feed.SyncIntervalMinutes.Should().Be(60);
        feed.LastSyncStatus.Should().Be(FeedSyncStatus.None);
        feed.LastSyncAt.Should().BeNull();
        feed.LastSyncProductCount.Should().Be(0);
    }

    // ════ 2. Percent markup — 20% on 100 = 120 ════

    [Fact]
    public void ApplyMarkup_PercentMode_CalculatesCorrectly()
    {
        // Arrange
        var feed = CreateFeed(usePercent: true, percentMarkup: 20m);

        // Act
        var result = feed.ApplyMarkup(100m);

        // Assert
        result.Should().Be(120m); // 100 * (1 + 20/100) = 120
    }

    // ════ 3. Fixed markup — 50 TL on 100 = 150 ════

    [Fact]
    public void ApplyMarkup_FixedMode_CalculatesCorrectly()
    {
        // Arrange
        var feed = CreateFeed(usePercent: false, fixedMarkup: 50m);

        // Act
        var result = feed.ApplyMarkup(100m);

        // Assert
        result.Should().Be(150m); // 100 + 50 = 150
    }

    // ════ 4. RecordSyncResult — success sets Completed + raises event ════

    [Fact]
    public void RecordSyncResult_Success_SetsCompletedAndRaisesEvent()
    {
        // Arrange
        var feed = CreateFeed();

        // Act
        feed.RecordSyncResult(totalProducts: 250, updatedProducts: 45, deactivatedProducts: 3);

        // Assert
        feed.LastSyncStatus.Should().Be(FeedSyncStatus.Completed);
        feed.LastSyncProductCount.Should().Be(250);
        feed.LastSyncUpdatedCount.Should().Be(45);
        feed.LastSyncDeactivatedCount.Should().Be(3);
        feed.LastSyncError.Should().BeNull();
        feed.LastSyncAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        feed.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SupplierFeedSyncedEvent>();

        var evt = (SupplierFeedSyncedEvent)feed.DomainEvents.First();
        evt.TotalProducts.Should().Be(250);
        evt.UpdatedProducts.Should().Be(45);
        evt.DeactivatedProducts.Should().Be(3);
        evt.Status.Should().Be(FeedSyncStatus.Completed);
    }

    // ════ 5. RecordSyncResult — with error sets PartiallyCompleted ════

    [Fact]
    public void RecordSyncResult_WithError_SetsPartiallyCompleted()
    {
        // Arrange
        var feed = CreateFeed();

        // Act
        feed.RecordSyncResult(
            totalProducts: 250,
            updatedProducts: 100,
            deactivatedProducts: 0,
            error: "Timeout at product 101");

        // Assert
        feed.LastSyncStatus.Should().Be(FeedSyncStatus.PartiallyCompleted);
        feed.LastSyncError.Should().Be("Timeout at product 101");
        feed.LastSyncProductCount.Should().Be(250);
        feed.LastSyncUpdatedCount.Should().Be(100);

        var evt = (SupplierFeedSyncedEvent)feed.DomainEvents.First();
        evt.Status.Should().Be(FeedSyncStatus.PartiallyCompleted);
    }
}
