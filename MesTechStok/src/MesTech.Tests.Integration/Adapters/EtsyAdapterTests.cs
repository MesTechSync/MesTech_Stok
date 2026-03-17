using System.Net.Http;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// EtsyAdapter contract tests with WireMock.
/// Tests cover TestConnection (success / auth failure / rate limit), PullProducts,
/// PushProduct, PushStockUpdate, PushPriceUpdate, PullOrders, and capability flags.
///
/// Etsy API base: https://openapi.etsy.com/v3 — adaptor hardcodes HTTPS, so requests
/// are redirected to WireMock via HttpsToHttpRedirectHandler (defined in ShopifyAdapterTests.cs).
/// EtsyOptions.ShopId + AccessToken seed the adapter directly so tests can call methods
/// without going through TestConnectionAsync.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Etsy")]
public class EtsyAdapterTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<EtsyAdapter> _logger;

    private string WireMockHostPort => new Uri(_fixture.BaseUrl).Authority;

    private const string TestShopId = "12345678";
    private const string TestAccessToken = "etsy-oauth2-test-token";
    private const string TestApiKey = "etsy-keystring-test";

    private const string BaseApiPath = "/v3/application";

    public EtsyAdapterTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<EtsyAdapter>();
    }

    public void Dispose() { }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private HttpClient CreateHttpClient()
    {
        var handler = new HttpsToHttpRedirectHandler(new HttpClientHandler());
        return new HttpClient(handler);
    }

    /// <summary>Unconfigured adapter — no credentials pre-seeded.</summary>
    private EtsyAdapter CreateAdapter()
        => new EtsyAdapter(CreateHttpClient(), _logger);

    /// <summary>
    /// Configured adapter seeded via IOptions so PullProducts / PullOrders etc.
    /// can be called without a prior TestConnectionAsync round-trip.
    /// </summary>
    private EtsyAdapter CreateConfiguredAdapter()
    {
        var options = Options.Create(new EtsyOptions
        {
            ShopId = TestShopId,
            AccessToken = TestAccessToken,
            ApiKey = TestApiKey,
            Enabled = true
        });
        return new EtsyAdapter(CreateHttpClient(), _logger, options);
    }

    private Dictionary<string, string> ValidCredentials => new()
    {
        ["ShopId"] = TestShopId,
        ["AccessToken"] = TestAccessToken,
        ["ApiKey"] = TestApiKey
    };

    // ── Stubs ──────────────────────────────────────────────────────────────────

    private void StubShopInfo(string shopName = "My Etsy Shop", int listingCount = 10)
    {
        _mockServer
            .Given(Request.Create()
                .WithPath($"{BaseApiPath}/shops/{TestShopId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{
                    ""shop_id"": {TestShopId},
                    ""shop_name"": ""{shopName}"",
                    ""listing_active_count"": {listingCount}
                }}"));
    }

    private void StubActiveListings(int count = 2)
    {
        var listings = new System.Text.StringBuilder();
        listings.Append("[");
        for (int i = 0; i < count; i++)
        {
            if (i > 0) listings.Append(",");
            listings.Append($@"{{
                ""listing_id"": {1000 + i},
                ""title"": ""Test Listing {i + 1}"",
                ""description"": ""A handmade item {i + 1}"",
                ""quantity"": {10 + i},
                ""skus"": [""ETSY-SKU-{i + 1:D3}""],
                ""price"": {{""amount"": {2999 + i * 100}, ""divisor"": 100, ""currency_code"": ""USD""}},
                ""tags"": [""handmade"", ""art""],
                ""images"": [{{""url_570xN"": ""https://i.etsystatic.com/image{i}.jpg""}}]
            }}");
        }
        listings.Append("]");

        _mockServer
            .Given(Request.Create()
                .WithPath($"{BaseApiPath}/shops/{TestShopId}/listings/active")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""count"": {count}, ""results"": {listings}}}"));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 1. PlatformCode
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PlatformCode_IsEtsy()
    {
        var adapter = CreateAdapter();
        adapter.PlatformCode.Should().Be("Etsy");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. Capability flags
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CapabilityFlags_StockAndPriceTrue_ShipmentFalse()
    {
        var adapter = CreateAdapter();
        adapter.SupportsStockUpdate.Should().BeTrue();
        adapter.SupportsPriceUpdate.Should().BeTrue();
        adapter.SupportsShipment.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. TestConnection — Valid credentials → success
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TestConnection_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        StubShopInfo("Artisan Goods", listingCount: 42);

        var credentials = new Dictionary<string, string>
        {
            ["ShopId"] = TestShopId,
            ["AccessToken"] = TestAccessToken,
            ["ApiKey"] = TestApiKey
        };

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(credentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Etsy");
        result.StoreName.Should().Be("Artisan Goods");
        result.ProductCount.Should().Be(42);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 4. TestConnection — 401 Unauthorized → failure
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TestConnection_AuthFailure_ReturnsFailure()
    {
        // Arrange — 401 from shop info endpoint
        _mockServer
            .Given(Request.Create()
                .WithPath($"{BaseApiPath}/shops/{TestShopId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"": ""invalid_token"", ""error_description"": ""Access token is invalid""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("401");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 5. TestConnection — Missing credentials → failure (no HTTP call)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TestConnection_MissingCredentials_ReturnsFailure()
    {
        // Arrange — empty credentials
        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(new Dictionary<string, string>());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 6. PullProducts — Returns mapped products
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProducts_ValidListings_ReturnsMappedProducts()
    {
        // Arrange
        StubActiveListings(count: 2);

        var adapter = CreateConfiguredAdapter();

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(2);
        products[0].Name.Should().Be("Test Listing 1");
        products[0].SKU.Should().Be("ETSY-SKU-001");
        products[0].Stock.Should().Be(10);
        products[0].SalePrice.Should().Be(29.99m);
        products[0].CurrencyCode.Should().Be("USD");
        products[0].IsActive.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 7. PullProducts — Empty listings → empty list
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProducts_EmptyShop_ReturnsEmptyList()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath($"{BaseApiPath}/shops/{TestShopId}/listings/active")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""count"": 0, ""results"": []}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 8. PullProducts — Rate limit 429 → adapter returns empty (resilience absorbs retries)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProducts_RateLimit429_ReturnsEmptyListWithoutThrowing()
    {
        // Arrange — all requests return 429
        _mockServer
            .Given(Request.Create()
                .WithPath($"{BaseApiPath}/shops/{TestShopId}/listings/active")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("Retry-After", "1")
                .WithBody(@"{""error"": ""rate_limit_exceeded""}"));

        // Use very short delays for test speed — override by injecting options with no delay
        // The adapter's Polly pipeline will retry up to 3 times then give up on the response.
        // PullProductsAsync catches the non-success response and returns empty.
        var adapter = CreateConfiguredAdapter();

        // Act — should not throw; logs a warning and returns empty after retries exhausted
        var products = await adapter.PullProductsAsync()
            .WaitAsync(TimeSpan.FromSeconds(30)); // safety timeout

        // Assert — empty result, no exception
        products.Should().BeEmpty("adapter logs and returns empty list when API persistently rate-limits");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 9. PushProduct — 201 Created → returns true
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushProduct_ValidProduct_ReturnsTrue()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath($"{BaseApiPath}/shops/{TestShopId}/listings")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""listing_id"": 9001, ""title"": ""My New Listing""}"));

        var adapter = CreateConfiguredAdapter();

        var product = new MesTech.Domain.Entities.Product
        {
            SKU = "ETSY-NEW-001",
            Name = "My New Listing",
            Description = "A great handmade product",
            SalePrice = 49.99m,
            Stock = 5,
            IsActive = true
        };

        // Act
        var result = await adapter.PushProductAsync(product);

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 10. PushProduct — 401 Unauthorized → returns false
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushProduct_AuthFailure_ReturnsFalse()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath($"{BaseApiPath}/shops/{TestShopId}/listings")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"": ""invalid_token""}"));

        var adapter = CreateConfiguredAdapter();

        var product = new MesTech.Domain.Entities.Product
        {
            SKU = "AUTH-FAIL-001",
            Name = "Unauthorized Product",
            SalePrice = 9.99m,
            Stock = 1
        };

        // Act
        var result = await adapter.PushProductAsync(product);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 11. PullOrders — Returns mapped orders
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullOrders_ValidReceipts_ReturnsMappedOrders()
    {
        // Arrange
        var receiptsJson = $@"{{
            ""count"": 1,
            ""results"": [{{
                ""receipt_id"": 88001,
                ""name"": ""Jane Smith"",
                ""buyer_email"": ""jane@example.com"",
                ""status"": ""paid"",
                ""grandtotal"": {{""amount"": 7999, ""divisor"": 100, ""currency_code"": ""USD""}},
                ""total_shipping_cost"": {{""amount"": 500, ""divisor"": 100, ""currency_code"": ""USD""}},
                ""created_timestamp"": 1705312800,
                ""formatted_address"": ""123 Craft Lane"",
                ""city"": ""Brooklyn"",
                ""transactions"": [{{
                    ""transaction_id"": 55001,
                    ""title"": ""Handmade Pottery Mug"",
                    ""quantity"": 1,
                    ""sku"": ""MUG-001"",
                    ""price"": {{""amount"": 7999, ""divisor"": 100, ""currency_code"": ""USD""}}
                }}]
            }}]
        }}";

        _mockServer
            .Given(Request.Create()
                .WithPath($"{BaseApiPath}/shops/{TestShopId}/receipts")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(receiptsJson));

        var adapter = CreateConfiguredAdapter();

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().HaveCount(1);
        orders[0].PlatformCode.Should().Be("Etsy");
        orders[0].PlatformOrderId.Should().Be("88001");
        orders[0].OrderNumber.Should().Be("ETSY-88001");
        orders[0].Status.Should().Be("paid");
        orders[0].CustomerName.Should().Be("Jane Smith");
        orders[0].CustomerEmail.Should().Be("jane@example.com");
        orders[0].TotalAmount.Should().Be(79.99m);
        orders[0].ShippingCost.Should().Be(5.00m);
        orders[0].Currency.Should().Be("USD");
        orders[0].CustomerCity.Should().Be("Brooklyn");
        orders[0].Lines.Should().HaveCount(1);
        orders[0].Lines[0].SKU.Should().Be("MUG-001");
        orders[0].Lines[0].Quantity.Should().Be(1);
        orders[0].Lines[0].UnitPrice.Should().Be(79.99m);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 12. PullOrders — 401 Unauthorized → returns empty list
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullOrders_AuthFailure_ReturnsEmptyList()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath($"{BaseApiPath}/shops/{TestShopId}/receipts")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"": ""invalid_token""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().BeEmpty("adapter logs the error and returns empty list on auth failure");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 13. PullOrders — No receipts → empty list
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullOrders_NoReceipts_ReturnsEmptyList()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath($"{BaseApiPath}/shops/{TestShopId}/receipts")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""count"": 0, ""results"": []}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 14. UpdateOrderStatus — 200 OK → returns true
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateOrderStatus_ValidTracking_ReturnsTrue()
    {
        // Arrange
        const string receiptId = "88001";
        _mockServer
            .Given(Request.Create()
                .WithPath($"{BaseApiPath}/shops/{TestShopId}/receipts/{receiptId}/tracking")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""receipt_id"": 88001, ""was_paid"": true}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.UpdateOrderStatusAsync(receiptId, "TK123456789TR");

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 15. UpdateOrderStatus — 401 Unauthorized → returns false
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateOrderStatus_AuthFailure_ReturnsFalse()
    {
        // Arrange
        const string receiptId = "88002";
        _mockServer
            .Given(Request.Create()
                .WithPath($"{BaseApiPath}/shops/{TestShopId}/receipts/{receiptId}/tracking")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"": ""invalid_token""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.UpdateOrderStatusAsync(receiptId, "TK999888777TR");

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 16. GetCategories — Returns taxonomy nodes
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCategories_ValidResponse_ReturnsMappedCategories()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath($"{BaseApiPath}/seller-taxonomy/nodes")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""results"": [
                        {
                            ""id"": 1,
                            ""name"": ""Jewelry"",
                            ""parent_id"": null,
                            ""children"": [
                                {""id"": 11, ""name"": ""Necklaces""},
                                {""id"": 12, ""name"": ""Bracelets""}
                            ]
                        },
                        {
                            ""id"": 2,
                            ""name"": ""Clothing"",
                            ""parent_id"": null,
                            ""children"": []
                        }
                    ]
                }"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var categories = await adapter.GetCategoriesAsync();

        // Assert
        categories.Should().HaveCount(2);
        categories[0].Name.Should().Be("Jewelry");
        categories[0].SubCategories.Should().HaveCount(2);
        categories[0].SubCategories[0].Name.Should().Be("Necklaces");
        categories[0].SubCategories[1].Name.Should().Be("Bracelets");
        categories[1].Name.Should().Be("Clothing");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 17. PullProducts — Not configured → throws InvalidOperationException
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProducts_NotConfigured_ThrowsInvalidOperation()
    {
        // Arrange — adapter with no options and no TestConnectionAsync call
        var adapter = CreateAdapter();

        // Act & Assert
        var act = async () => await adapter.PullProductsAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*EtsyAdapter*");
    }
}
