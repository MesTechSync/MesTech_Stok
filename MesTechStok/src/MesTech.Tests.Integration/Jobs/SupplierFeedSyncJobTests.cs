using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Integration.Jobs;

/// <summary>
/// DEV 5 — Dalga 7.5 Task 5.02: SupplierFeedSyncJob integration tests.
/// Skip'd until DEV 1 implements SupplierFeedSyncJob (Görev 1.02) +
/// DEV 3 implements feed parsers (Görev 3.01).
/// Tests use WireMock for mock feed URL.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "SyncJob")]
public class SupplierFeedSyncJobTests
{
    private const string SkipReason = "DEV 1 SupplierFeedSyncJob + DEV 3 parser implementasyonu bekleniyor (Görev 1.02 + 3.01)";

    [Fact(Skip = SkipReason)]
    public async Task SyncJob_WithValidFeed_CreatesProducts()
    {
        // Setup:
        // 1. WireMock: serve sample XML feed at /feed/test.xml
        // 2. SupplierFeed entity: FeedUrl = WireMock URL, Format = Xml
        // 3. Execute SupplierFeedSyncJob.ExecuteAsync(feedId)
        //
        // Assert:
        // - Products created in DB with correct SKU, name, price
        // - SupplierFeed.LastSyncStatus == Completed
        // - SupplierFeed.LastSyncProductCount == 3
        Assert.True(true, "Placeholder — activate when DEV 1 + DEV 3 complete");
    }

    [Fact(Skip = SkipReason)]
    public async Task SyncJob_WithStockZero_DeactivatesProduct()
    {
        // Setup:
        // 1. Existing product with Stock > 0
        // 2. Feed returns Stock = 0 for that product
        // 3. SupplierFeed.AutoDeactivateOnZeroStock = true
        //
        // Assert:
        // - Product.IsActive == false
        // - SupplierFeed.LastSyncDeactivatedCount >= 1
        Assert.True(true, "Placeholder — activate when DEV 1 + DEV 3 complete");
    }

    [Fact(Skip = SkipReason)]
    public async Task SyncJob_WithRestock_ReactivatesProduct()
    {
        // Setup:
        // 1. Existing product with IsActive = false, Stock = 0
        // 2. Feed returns Stock > 0 for that product
        // 3. SupplierFeed.AutoActivateOnRestock = true
        //
        // Assert:
        // - Product.IsActive == true
        // - Product.Stock == feed quantity
        Assert.True(true, "Placeholder — activate when DEV 1 + DEV 3 complete");
    }

    [Fact(Skip = SkipReason)]
    public async Task SyncJob_WithMarkup_AppliesPriceCorrectly()
    {
        // Setup:
        // 1. SupplierFeed: UsePercentMarkup = true, PriceMarkupPercent = 30
        // 2. Feed product price = 100.00
        //
        // Assert:
        // - Product.SalePrice == 130.00 (100 * 1.30)
        //
        // Also test fixed markup:
        // - UsePercentMarkup = false, PriceMarkupFixed = 25
        // - Product.SalePrice == 125.00 (100 + 25)
        Assert.True(true, "Placeholder — activate when DEV 1 + DEV 3 complete");
    }

    [Fact(Skip = SkipReason)]
    public async Task SyncJob_PublishesEvent()
    {
        // Setup:
        // 1. Valid feed + SupplierFeed entity
        // 2. Execute sync job
        //
        // Assert:
        // - SupplierFeedSyncedEvent published via MediatR
        // - Event contains correct SupplierId, ProductCount, UpdatedCount
        // Use MediatR Verify or capture published events
        Assert.True(true, "Placeholder — activate when DEV 1 + DEV 3 complete");
    }

    /// <summary>
    /// Non-Skip'd test: Verify SupplierFeed.ApplyMarkup domain logic.
    /// This works WITHOUT DEV 1's job implementation.
    /// </summary>
    [Theory]
    [InlineData(100.00, true, 30.0, 0.0, 130.00)]   // 100 * 1.30
    [InlineData(100.00, true, 50.0, 0.0, 150.00)]   // 100 * 1.50
    [InlineData(100.00, false, 0.0, 25.0, 125.00)]  // 100 + 25
    [InlineData(200.00, true, 10.0, 0.0, 220.00)]   // 200 * 1.10
    [InlineData(0.00, true, 30.0, 0.0, 0.00)]       // 0 * 1.30 = 0
    public void SupplierFeed_ApplyMarkup_CalculatesCorrectly(
        decimal originalPrice, bool usePercent, decimal percent, decimal fixedAmount, decimal expected)
    {
        var feed = new SupplierFeed
        {
            UsePercentMarkup = usePercent,
            PriceMarkupPercent = percent,
            PriceMarkupFixed = fixedAmount
        };

        feed.ApplyMarkup(originalPrice).Should().Be(expected);
    }

    /// <summary>
    /// Non-Skip'd test: Verify SupplierFeed.RecordSyncResult domain logic.
    /// </summary>
    [Fact]
    public void SupplierFeed_RecordSyncResult_UpdatesFieldsAndRaisesEvent()
    {
        var feed = new SupplierFeed
        {
            Name = "Test Feed",
            SupplierId = Guid.NewGuid()
        };

        feed.RecordSyncResult(100, 25, 5);

        feed.LastSyncStatus.Should().Be(FeedSyncStatus.Completed);
        feed.LastSyncProductCount.Should().Be(100);
        feed.LastSyncUpdatedCount.Should().Be(25);
        feed.LastSyncDeactivatedCount.Should().Be(5);
        feed.LastSyncError.Should().BeNull();
        feed.LastSyncAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        feed.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void SupplierFeed_RecordSyncResult_WithError_SetsPartiallyCompleted()
    {
        var feed = new SupplierFeed
        {
            Name = "Error Feed",
            SupplierId = Guid.NewGuid()
        };

        feed.RecordSyncResult(50, 10, 0, "Network timeout");

        feed.LastSyncStatus.Should().Be(FeedSyncStatus.PartiallyCompleted);
        feed.LastSyncError.Should().Be("Network timeout");
    }
}
