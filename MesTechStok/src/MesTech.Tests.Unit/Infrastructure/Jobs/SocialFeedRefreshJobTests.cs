using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.Jobs;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure.Jobs;

/// <summary>
/// DEV 5 — G436: SocialFeedRefreshJob unit tests.
/// Job depends on IDbContextFactory&lt;AppDbContext&gt; so tests use InMemory DB via factory mock.
/// These tests cover: constructor guards, domain entity logic, adapter dispatch,
/// error recording, and no-config skip.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
public class SocialFeedRefreshJobTests
{
    // ── Constructor guards ──

    [Fact]
    public void Constructor_NullDbContext_Throws()
    {
        var act = () => new SocialFeedRefreshJob(
            null!,
            Enumerable.Empty<ISocialFeedAdapter>(),
            Mock.Of<ILogger<SocialFeedRefreshJob>>());
        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContextFactory");
    }

    [Fact]
    public void Constructor_NullAdapters_Throws()
    {
        var (factory, db) = CreateDbContextFactoryWithDb();
        using (db)
        {
            var act = () => new SocialFeedRefreshJob(
                factory,
                null!,
                Mock.Of<ILogger<SocialFeedRefreshJob>>());
            act.Should().Throw<ArgumentNullException>().WithParameterName("adapters");
        }
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var (factory, db) = CreateDbContextFactoryWithDb();
        using (db)
        {
            var act = () => new SocialFeedRefreshJob(
                factory,
                Enumerable.Empty<ISocialFeedAdapter>(),
                null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }
    }

    // ── No configs = skip ──

    [Fact]
    public async Task ExecuteAsync_NoActiveConfigs_SkipsGracefully()
    {
        var (factory, db) = CreateDbContextFactoryWithDb();
        using (db)
        {
            var adapter = CreateMockAdapter(SocialFeedPlatform.GoogleMerchant);

            var sut = new SocialFeedRefreshJob(factory, new[] { adapter.Object }, Mock.Of<ILogger<SocialFeedRefreshJob>>());
            await sut.ExecuteAsync();

            adapter.Verify(a => a.GenerateFeedAsync(It.IsAny<FeedGenerationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    // ── Adapter dispatch ──

    [Fact]
    public async Task ExecuteAsync_ActiveConfig_CallsMatchingAdapter()
    {
        var tenantId = Guid.NewGuid();
        var (factory, db) = CreateDbContextFactoryWithDb(tenantId);
        using (db)
        {
            var config = SocialFeedConfiguration.Create(tenantId, SocialFeedPlatform.GoogleMerchant);
            db.Set<SocialFeedConfiguration>().Add(config);
            await db.SaveChangesAsync();

            var adapter = CreateMockAdapter(SocialFeedPlatform.GoogleMerchant, success: true, itemCount: 42);

            var sut = new SocialFeedRefreshJob(factory, new[] { adapter.Object }, Mock.Of<ILogger<SocialFeedRefreshJob>>());
            await sut.ExecuteAsync();

            adapter.Verify(a => a.GenerateFeedAsync(
                It.Is<FeedGenerationRequest>(r => r.StoreId == config.TenantId),
                It.IsAny<CancellationToken>()), Times.Once);

            // Verify config was updated
            var updated = await db.Set<SocialFeedConfiguration>().FindAsync(config.Id);
            updated!.ItemCount.Should().Be(42);
            updated.LastGeneratedAt.Should().NotBeNull();
            updated.LastError.Should().BeNull();
        }
    }

    // ── No matching adapter = error recorded ──

    [Fact]
    public async Task ExecuteAsync_NoMatchingAdapter_RecordsError()
    {
        var tenantId = Guid.NewGuid();
        var (factory, db) = CreateDbContextFactoryWithDb(tenantId);
        using (db)
        {
            var config = SocialFeedConfiguration.Create(tenantId, SocialFeedPlatform.FacebookShop);
            db.Set<SocialFeedConfiguration>().Add(config);
            await db.SaveChangesAsync();

            // Only Google adapter registered, not Facebook
            var adapter = CreateMockAdapter(SocialFeedPlatform.GoogleMerchant);

            var sut = new SocialFeedRefreshJob(factory, new[] { adapter.Object }, Mock.Of<ILogger<SocialFeedRefreshJob>>());
            await sut.ExecuteAsync();

            var updated = await db.Set<SocialFeedConfiguration>().FindAsync(config.Id);
            updated!.LastError.Should().Contain("No adapter registered");
        }
    }

    // ── Adapter throws = error recorded, no crash ──

    [Fact]
    public async Task ExecuteAsync_AdapterThrows_RecordsErrorAndContinues()
    {
        var tenantId = Guid.NewGuid();
        var (factory, db) = CreateDbContextFactoryWithDb(tenantId);
        using (db)
        {
            var config = SocialFeedConfiguration.Create(tenantId, SocialFeedPlatform.GoogleMerchant);
            db.Set<SocialFeedConfiguration>().Add(config);
            await db.SaveChangesAsync();

            var adapter = new Mock<ISocialFeedAdapter>();
            adapter.Setup(a => a.Platform).Returns(SocialFeedPlatform.GoogleMerchant);
            adapter.Setup(a => a.GenerateFeedAsync(It.IsAny<FeedGenerationRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Feed API down"));

            var sut = new SocialFeedRefreshJob(factory, new[] { adapter.Object }, Mock.Of<ILogger<SocialFeedRefreshJob>>());
            await sut.ExecuteAsync(); // should NOT throw

            var updated = await db.Set<SocialFeedConfiguration>().FindAsync(config.Id);
            updated!.LastError.Should().Contain("Feed API down");
        }
    }

    // ── Feed generation failure (Success=false) ──

    [Fact]
    public async Task ExecuteAsync_FeedGenerationFails_RecordsError()
    {
        var tenantId = Guid.NewGuid();
        var (factory, db) = CreateDbContextFactoryWithDb(tenantId);
        using (db)
        {
            var config = SocialFeedConfiguration.Create(tenantId, SocialFeedPlatform.GoogleMerchant);
            db.Set<SocialFeedConfiguration>().Add(config);
            await db.SaveChangesAsync();

            var adapter = new Mock<ISocialFeedAdapter>();
            adapter.Setup(a => a.Platform).Returns(SocialFeedPlatform.GoogleMerchant);
            adapter.Setup(a => a.GenerateFeedAsync(It.IsAny<FeedGenerationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FeedGenerationResult(
                    Success: false,
                    FeedUrl: null,
                    ItemCount: 0,
                    GeneratedAt: DateTime.UtcNow,
                    Errors: new List<string> { "Invalid product data" }.AsReadOnly()));

            var sut = new SocialFeedRefreshJob(factory, new[] { adapter.Object }, Mock.Of<ILogger<SocialFeedRefreshJob>>());
            await sut.ExecuteAsync();

            var updated = await db.Set<SocialFeedConfiguration>().FindAsync(config.Id);
            updated!.LastError.Should().Contain("Invalid product data");
        }
    }

    // ── Inactive configs are excluded ──

    [Fact]
    public async Task ExecuteAsync_InactiveConfig_NotProcessed()
    {
        var tenantId = Guid.NewGuid();
        var (factory, db) = CreateDbContextFactoryWithDb(tenantId);
        using (db)
        {
            var config = SocialFeedConfiguration.Create(tenantId, SocialFeedPlatform.GoogleMerchant);
            config.IsActive = false;
            db.Set<SocialFeedConfiguration>().Add(config);
            await db.SaveChangesAsync();

            var adapter = CreateMockAdapter(SocialFeedPlatform.GoogleMerchant);

            var sut = new SocialFeedRefreshJob(factory, new[] { adapter.Object }, Mock.Of<ILogger<SocialFeedRefreshJob>>());
            await sut.ExecuteAsync();

            adapter.Verify(a => a.GenerateFeedAsync(It.IsAny<FeedGenerationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    // ── Domain entity logic ──

    [Fact]
    public void SocialFeedConfiguration_Create_ValidInput_Succeeds()
    {
        var tenantId = Guid.NewGuid();
        var config = SocialFeedConfiguration.Create(tenantId, SocialFeedPlatform.GoogleMerchant, TimeSpan.FromHours(3), "Elektronik,Giyim");

        config.TenantId.Should().Be(tenantId);
        config.Platform.Should().Be(SocialFeedPlatform.GoogleMerchant);
        config.RefreshInterval.Should().Be(TimeSpan.FromHours(3));
        config.CategoryFilter.Should().Be("Elektronik,Giyim");
        config.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SocialFeedConfiguration_Create_EmptyTenant_Throws()
    {
        var act = () => SocialFeedConfiguration.Create(Guid.Empty, SocialFeedPlatform.GoogleMerchant);
        act.Should().Throw<ArgumentException>().WithParameterName("tenantId");
    }

    [Fact]
    public void SocialFeedConfiguration_RecordGeneration_UpdatesFields()
    {
        var config = SocialFeedConfiguration.Create(Guid.NewGuid(), SocialFeedPlatform.FacebookShop);
        config.RecordGeneration("https://feed.example.com/123.xml", 150);

        config.FeedUrl.Should().Be("https://feed.example.com/123.xml");
        config.ItemCount.Should().Be(150);
        config.LastGeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        config.LastError.Should().BeNull();
    }

    [Fact]
    public void SocialFeedConfiguration_RecordError_TruncatesLongMessage()
    {
        var config = SocialFeedConfiguration.Create(Guid.NewGuid(), SocialFeedPlatform.GoogleMerchant);
        config.RecordError("Short error");

        config.LastError.Should().Be("Short error");
    }

    [Fact]
    public void SocialFeedConfiguration_NeedsRefresh_NoLastGeneration_ReturnsTrue()
    {
        var config = SocialFeedConfiguration.Create(Guid.NewGuid(), SocialFeedPlatform.GoogleMerchant);
        config.NeedsRefresh.Should().BeTrue();
    }

    [Fact]
    public void SocialFeedConfiguration_NeedsRefresh_RecentGeneration_ReturnsFalse()
    {
        var config = SocialFeedConfiguration.Create(Guid.NewGuid(), SocialFeedPlatform.GoogleMerchant);
        config.RecordGeneration("url", 10);
        config.NeedsRefresh.Should().BeFalse();
    }

    // ── Helpers ──

    private static AppDbContext CreateInMemoryDb(Guid? tenantId = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(tenantId ?? Guid.NewGuid());
        return new AppDbContext(options, tenantProvider.Object);
    }

    private static (IDbContextFactory<AppDbContext> Factory, AppDbContext Db) CreateDbContextFactoryWithDb(Guid? tenantId = null)
    {
        var db = CreateInMemoryDb(tenantId);
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(db);
        return (factory.Object, db);
    }

    private static Mock<ISocialFeedAdapter> CreateMockAdapter(
        SocialFeedPlatform platform, bool success = true, int itemCount = 10)
    {
        var mock = new Mock<ISocialFeedAdapter>();
        mock.Setup(a => a.Platform).Returns(platform);
        mock.Setup(a => a.GenerateFeedAsync(It.IsAny<FeedGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeedGenerationResult(
                Success: success,
                FeedUrl: success ? $"https://feed.example.com/{platform}.xml" : null,
                ItemCount: itemCount,
                GeneratedAt: DateTime.UtcNow));
        return mock;
    }
}
