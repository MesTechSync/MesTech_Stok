using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Feed;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// InstagramShopFeedAdapter entegrasyon testleri.
/// InstagramShopFeedAdapter, FacebookShopFeedAdapter'in ince bir subclass'idir;
/// ayni Facebook Commerce Manager catalog API'yi kullanir, sadece Platform farki vardir.
///
/// Bu test dosyasi:
/// - Platform farklilasmasini (InstagramShop vs FacebookShop) dogrular.
/// - Adapter'in Facebook temel siniftan devraldigini ve dogru calistirdigini dogrular.
/// - Her test farkli bir InMemory DB kullanir (IntegrationTestBase).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feed", "InstagramShop")]
public class InstagramShopFeedAdapterTests : IntegrationTestBase
{
    private static readonly Guid TestTenantId = Guid.Parse("16000000-0000-0000-0000-000000000001");

    private readonly InstagramShopFeedAdapter _adapter;
    private readonly FacebookShopFeedAdapter _facebookAdapter;

    public InstagramShopFeedAdapterTests()
    {
        SetCurrentTenant(TestTenantId);

        _adapter = new InstagramShopFeedAdapter(
            ContextFactory,
            new LoggerFactory().CreateLogger<InstagramShopFeedAdapter>());

        _facebookAdapter = new FacebookShopFeedAdapter(
            ContextFactory,
            new LoggerFactory().CreateLogger<FacebookShopFeedAdapter>());
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static FeedGenerationRequest MakeRequest(Guid? storeId = null)
        => new FeedGenerationRequest(storeId ?? TestTenantId, null, "TRY", "tr");

    private Product AddProduct(
        string sku,
        string name,
        decimal price,
        int stock = 10,
        string? imageUrl = null)
    {
        var product = new Product
        {
            TenantId = TestTenantId,
            SKU = sku,
            Name = name,
            Description = $"Description for {name}",
            SalePrice = price,
            Stock = stock,
            ImageUrl = imageUrl ?? "https://cdn.mestech.app/images/product.jpg",
            Brand = "TestBrand",
            IsActive = true,
            CategoryId = Guid.NewGuid()
        };
        Context.Products.Add(product);
        Context.SaveChanges();
        return product;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 1. Platform — InstagramShop, not FacebookShop
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Platform_IsInstagramShop()
    {
        _adapter.Platform.Should().Be(SocialFeedPlatform.InstagramShop);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. Platform differentiation — Instagram != Facebook
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Platform_DiffersFromFacebookShop()
    {
        _adapter.Platform.Should().NotBe(SocialFeedPlatform.FacebookShop,
            "Instagram and Facebook are distinct platforms even though they share the same feed format");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. GenerateFeed — Success returns valid result
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateFeed_SingleProduct_ReturnsSuccess()
    {
        // Arrange
        AddProduct("IG-SKU-001", "Instagram Test Urunu", 199.99m, stock: 15);
        var request = MakeRequest();

        // Act
        var result = await _adapter.GenerateFeedAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.ItemCount.Should().Be(1);
        result.FeedUrl.Should().NotBeNullOrEmpty();
        result.Errors.Should().BeNullOrEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 4. GenerateFeed — Feed URL contains Instagram platform identifier
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateFeed_FeedUrl_ContainsInstagramPlatform()
    {
        // Arrange
        AddProduct("IG-URL-001", "URL Test Urunu", 50.00m);
        var request = MakeRequest();

        // Act
        var result = await _adapter.GenerateFeedAsync(request);

        // Assert — URL must identify platform as "instagramshop" not "facebookshop"
        result.FeedUrl.Should().Contain("instagramshop",
            "Instagram feed URL must use the Instagram platform identifier");
        result.FeedUrl.Should().NotContain("facebookshop",
            "Instagram adapter must not use Facebook platform path");
        result.FeedUrl.Should().Contain(TestTenantId.ToString("N"));
        result.FeedUrl.Should().EndWith(".xml");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 5. GenerateFeed — Empty store returns success with 0 items
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateFeed_EmptyStore_ReturnsZeroItems()
    {
        // Arrange — no products
        var request = MakeRequest();

        // Act
        var result = await _adapter.GenerateFeedAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.ItemCount.Should().Be(0);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 6. GenerateFeed — Multiple products all included
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateFeed_MultipleProducts_AllIncluded()
    {
        // Arrange
        for (int i = 1; i <= 3; i++)
            AddProduct($"IG-MULTI-{i:D3}", $"Urun {i}", price: 50m * i);

        var request = MakeRequest();

        // Act
        var result = await _adapter.GenerateFeedAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.ItemCount.Should().Be(3);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 7. GenerateFeed — Instagram and Facebook produce same item count for same products
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateFeed_SameDataAsFacebook_SameItemCount()
    {
        // Arrange — 2 products
        AddProduct("SHARED-001", "Shared Urun 1", 100.00m);
        AddProduct("SHARED-002", "Shared Urun 2", 200.00m);

        var request = MakeRequest();

        // Act — generate with both adapters against the same InMemory DB
        var igResult = await _adapter.GenerateFeedAsync(request);
        var fbResult = await _facebookAdapter.GenerateFeedAsync(request);

        // Assert — item counts must be identical (same data, same format)
        igResult.ItemCount.Should().Be(fbResult.ItemCount,
            "Instagram and Facebook adapters produce same item count for same product set");
        igResult.Success.Should().Be(fbResult.Success);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 8. GenerateFeed — Out of stock product included (not filtered)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateFeed_OutOfStockProduct_IncludedInFeed()
    {
        // Arrange — stock == 0
        AddProduct("IG-OOS-001", "Tukenmis Urun", 75.00m, stock: 0);
        var request = MakeRequest();

        // Act
        var result = await _adapter.GenerateFeedAsync(request);

        // Assert — out-of-stock items are included with appropriate availability tag
        result.Success.Should().BeTrue();
        result.ItemCount.Should().Be(1,
            "out-of-stock products must be included in Instagram feed with 'out of stock' availability");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 9. GetFeedStatus — After generation, status is healthy
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetFeedStatus_AfterGeneration_IsHealthy()
    {
        // Arrange
        AddProduct("IG-STATUS-001", "Durum Test Urunu", 89.99m);
        await _adapter.GenerateFeedAsync(MakeRequest());

        // Act
        var status = await _adapter.GetFeedStatusAsync();

        // Assert
        status.IsHealthy.Should().BeTrue();
        status.LastGenerated.Should().HaveValue();
        status.LastGenerated!.Value.Should()
            .BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        status.ItemCount.Should().Be(1);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 10. GetFeedStatus — Before generation, not healthy
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetFeedStatus_BeforeGeneration_IsNotHealthy()
    {
        // Act — fresh adapter, no generate call
        var status = await _adapter.GetFeedStatusAsync();

        // Assert
        status.IsHealthy.Should().BeFalse();
        status.LastGenerated.Should().BeNull();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 11. ValidateFeed — Valid URL returns valid
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ValidateFeed_ValidAbsoluteUrl_IsValid()
    {
        // Arrange
        var feedUrl = $"https://feeds.mestech.app/instagramshop/{TestTenantId:N}.xml";

        // Act
        var result = await _adapter.ValidateFeedAsync(feedUrl);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 12. ValidateFeed — Empty URL returns invalid
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ValidateFeed_EmptyUrl_IsInvalid()
    {
        // Act
        var result = await _adapter.ValidateFeedAsync(string.Empty);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 13. ScheduleRefresh — Sets next scheduled time
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ScheduleRefresh_SetsNextScheduled()
    {
        // Arrange
        AddProduct("IG-SCHED-001", "Zamanlama Test Urunu", 60.00m);
        await _adapter.GenerateFeedAsync(MakeRequest());

        var interval = TimeSpan.FromHours(8);
        var before = DateTime.UtcNow;

        // Act
        await _adapter.ScheduleRefreshAsync(interval);

        // Assert
        var status = await _adapter.GetFeedStatusAsync();
        status.NextScheduled.Should().HaveValue();
        status.NextScheduled!.Value.Should()
            .BeOnOrAfter(before.Add(interval).AddSeconds(-1));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 14. GenerateFeed — Only active products included
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateFeed_InactiveProducts_NotIncluded()
    {
        // Arrange
        AddProduct("IG-ACTIVE-001", "Active Urun", 80.00m, stock: 5);

        var inactive = new Product
        {
            TenantId = TestTenantId,
            SKU = "IG-INACTIVE-001",
            Name = "Inactive Urun",
            SalePrice = 80.00m,
            Stock = 5,
            IsActive = false,
            CategoryId = Guid.NewGuid()
        };
        Context.Products.Add(inactive);
        await Context.SaveChangesAsync();

        var request = MakeRequest();

        // Act
        var result = await _adapter.GenerateFeedAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.ItemCount.Should().Be(1, "inactive products must be excluded from Instagram feed");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 15. GenerateFeed — Special chars sanitized without error
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateFeed_SpecialCharsInName_NoError()
    {
        // Arrange
        var product = new Product
        {
            TenantId = TestTenantId,
            SKU = "IG-SPEC-001",
            Name = "Urun <Test> & Ozel",
            SalePrice = 15.00m,
            Stock = 2,
            IsActive = true,
            CategoryId = Guid.NewGuid()
        };
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var request = MakeRequest();

        // Act
        var result = await _adapter.GenerateFeedAsync(request);

        // Assert — adapter must succeed and include the product (special chars sanitized internally)
        result.Success.Should().BeTrue();
        result.ItemCount.Should().Be(1);
        result.Errors.Should().BeNullOrEmpty("sanitized special chars must not cause feed errors");

        // Verify the adapter's Sanitize method (shared with Facebook base) escapes XML special chars
        var sanitized = FacebookShopFeedAdapter.Sanitize("Urun <Test> & Ozel", 150);
        sanitized.Should().Contain("&lt;", "< must be escaped");
        sanitized.Should().Contain("&gt;", "> must be escaped");
        sanitized.Should().Contain("&amp;", "& must be escaped");
        sanitized.Should().NotContain("<", "raw < must not appear in sanitized output");
    }
}
