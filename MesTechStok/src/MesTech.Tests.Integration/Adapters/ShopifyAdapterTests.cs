using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
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
/// ShopifyAdapter contract tests with WireMock.
/// Tests cover PlatformCode, TestConnection, PullProducts (pagination via Link header),
/// PushStockUpdate, PushPriceUpdate, PullOrders, VerifyWebhookSignature, RegisterWebhook.
///
/// Because ShopifyAdapter builds absolute URLs as https://{ShopDomain}/admin/api/2024-01/...,
/// tests configure HttpClient with an HttpsToHttpRedirectHandler so requests are
/// transparently redirected to the HTTP WireMock server.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Shopify")]
public class ShopifyAdapterTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<ShopifyAdapter> _logger;

    // WireMock starts on e.g. http://127.0.0.1:PORT — extract host:port for ShopDomain
    private string WireMockHostPort => new Uri(_fixture.BaseUrl).Authority; // "127.0.0.1:PORT"

    private const string TestAccessToken = "shpat_test-access-token";
    private const string TestLocationId = "12345678";
    private const string TestWebhookSecret = "test-webhook-secret";
    private const string ApiVersion = "2024-01";

    private string AdminApiBase => $"/admin/api/{ApiVersion}";

    public ShopifyAdapterTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<ShopifyAdapter>();
    }

    public void Dispose()
    {
        // WireMockFixture handles server lifecycle
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an HttpClient with a delegating handler that rewrites https:// → http://
    /// so that requests to the adapter's constructed absolute HTTPS URLs reach WireMock's
    /// plain HTTP server.
    /// </summary>
    private HttpClient CreateHttpClient()
    {
        var handler = new HttpsToHttpRedirectHandler(new HttpClientHandler());
        return new HttpClient(handler);
    }

    /// <summary>
    /// Creates an unconfigured ShopifyAdapter (no credentials seeded).
    /// </summary>
    private ShopifyAdapter CreateAdapter()
    {
        return new ShopifyAdapter(CreateHttpClient(), _logger);
    }

    /// <summary>
    /// Creates a fully configured ShopifyAdapter seeded from IOptions, bypassing
    /// TestConnectionAsync so tests can directly call product/order methods.
    /// </summary>
    private ShopifyAdapter CreateConfiguredAdapter(string? webhookSecret = null)
    {
        var options = Options.Create(new ShopifyOptions
        {
            ShopDomain = WireMockHostPort,
            AccessToken = TestAccessToken,
            LocationId = TestLocationId,
            WebhookSecret = webhookSecret ?? TestWebhookSecret
        });

        return new ShopifyAdapter(CreateHttpClient(), _logger, options);
    }

    private Dictionary<string, string> ValidCredentials => new()
    {
        ["ShopDomain"] = WireMockHostPort,
        ["AccessToken"] = TestAccessToken,
        ["LocationId"] = TestLocationId,
        ["WebhookSecret"] = TestWebhookSecret
    };

    private void StubShopJson(string storeName = "Test Shopify Store")
    {
        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/shop.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""shop"":{{""id"":1,""name"":""{storeName}"",""email"":""test@shopify.com""}}}}"));
    }

    private void StubProductCountJson(int count = 42)
    {
        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/products/count.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""count"":{count}}}"));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 1. PlatformCode
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PlatformCode_IsShopify()
    {
        var adapter = CreateAdapter();
        adapter.PlatformCode.Should().Be("Shopify");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. TestConnection — Valid shop
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TestConnection_ValidShop_ReturnsSuccess()
    {
        // Arrange
        StubShopJson("My Test Store");
        StubProductCountJson(15);

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Shopify");
        result.StoreName.Should().Be("My Test Store");
        result.ProductCount.Should().Be(15);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. TestConnection — Network error / server unreachable
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TestConnection_NetworkError_ReturnsFailure()
    {
        // Arrange — stub shop.json to return 401 Unauthorized
        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/shop.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""errors"":""[API] Invalid API key or access token""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("401");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 4. PullProducts — Returns mapped products
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProducts_Returns_Mapped()
    {
        // Arrange
        var productsJson = @"{""products"":[{""id"":1001,""title"":""Blue T-Shirt"",""status"":""active"",
            ""variants"":[{""id"":2001,""sku"":""BTS-001"",""price"":""29.99"",""inventory_quantity"":50,
            ""inventory_item_id"":3001}]}]}";

        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/products.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(productsJson));

        var adapter = CreateConfiguredAdapter();

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(1);
        products[0].Name.Should().Be("Blue T-Shirt");
        products[0].SKU.Should().Be("BTS-001");
        products[0].SalePrice.Should().Be(29.99m);
        products[0].Stock.Should().Be(50);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 5. PullProducts — Empty shop returns empty list
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProducts_EmptyShop_ReturnsEmptyList()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/products.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""products"":[]}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 6. PullProducts — Pagination via Link header
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProducts_Pagination_HandledCorrectly()
    {
        // Arrange — page 1 returns Link header pointing to page 2
        var page1Body = @"{""products"":[{""id"":1001,""title"":""Product A"",
            ""variants"":[{""id"":2001,""sku"":""SKU-A"",""price"":""10.00"",
            ""inventory_quantity"":5,""inventory_item_id"":3001}]}]}";

        var page2Body = @"{""products"":[{""id"":1002,""title"":""Product B"",
            ""variants"":[{""id"":2002,""sku"":""SKU-B"",""price"":""20.00"",
            ""inventory_quantity"":3,""inventory_item_id"":3002}]}]}";

        // WireMock base URL for building Link header page_info URL
        var page2Url = $"https://{WireMockHostPort}{AdminApiBase}/products.json?limit=250&page_info=cursor-abc";

        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/products.json")
                .WithParam("limit", "250")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("Link", $@"<{page2Url}>; rel=""next""")
                .WithBody(page1Body));

        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/products.json")
                .WithParam("page_info", "cursor-abc")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                // No Link header — last page
                .WithBody(page2Body));

        var adapter = CreateConfiguredAdapter();

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — products from both pages returned
        products.Should().HaveCount(2);
        products.Select(p => p.SKU).Should().Contain(new[] { "SKU-A", "SKU-B" });
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 7. PushStockUpdate — Finds variant, updates level
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushStockUpdate_FindsVariant_UpdatesLevel()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var sku = productId.ToString();

        // Step 1: GET variants
        var variantsBody = $@"{{""variants"":[
            {{""id"":9001,""sku"":""{sku}"",""inventory_item_id"":8001}}
        ]}}";

        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/variants.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(variantsBody));

        // Step 2: POST inventory_levels/set.json
        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/inventory_levels/set.json")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""inventory_level"":{""inventory_item_id"":8001,""location_id"":12345678,""available"":100}}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 100);

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 8. PushStockUpdate — Variant not found, returns false
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushStockUpdate_VariantNotFound_ReturnsFalse()
    {
        // Arrange — variants list returns empty (no matching SKU)
        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/variants.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""variants"":[{""id"":9999,""sku"":""DIFFERENT-SKU"",""inventory_item_id"":8999}]}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 50);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 9. PushPriceUpdate — Finds variant, updates price
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushPriceUpdate_FindsVariant_UpdatesPrice()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var sku = productId.ToString();
        long variantId = 7001;

        // Step 1: GET variants (price update uses different fields query)
        var variantsBody = $@"{{""variants"":[
            {{""id"":{variantId},""sku"":""{sku}""}}
        ]}}";

        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/variants.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(variantsBody));

        // Step 2: PUT /variants/{id}.json
        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/variants/{variantId}.json")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""variant"":{{""id"":{variantId},""price"":""149.99""}}}}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 149.99m);

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 10. PushPriceUpdate — Variant not found, returns false
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushPriceUpdate_VariantNotFound_ReturnsFalse()
    {
        // Arrange — variants list returns empty variants array
        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/variants.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""variants"":[]}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.PushPriceUpdateAsync(Guid.NewGuid(), 99.99m);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 11. PullOrders — Returns orders
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullOrders_ReturnsOrders()
    {
        // Arrange
        var ordersBody = @"{""orders"":[{
            ""id"":5001,
            ""name"":""#1001"",
            ""financial_status"":""paid"",
            ""currency"":""USD"",
            ""total_price"":""199.99"",
            ""total_discounts"":""0.00"",
            ""created_at"":""2026-01-15T10:00:00Z"",
            ""customer"":{""id"":101,""first_name"":""John"",""last_name"":""Doe"",
                ""email"":""john@example.com"",""phone"":""+1234567890""},
            ""shipping_address"":{""address1"":""123 Main St"",""city"":""New York""},
            ""line_items"":[{""id"":6001,""sku"":""SKU-001"",""title"":""Blue T-Shirt"",
                ""quantity"":2,""price"":""99.99"",""total_discount"":""0.00"",
                ""tax_lines"":[{""rate"":0.1,""price"":""20.00"",""title"":""Tax""}]}]
        }]}";

        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/orders.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(ordersBody));

        var adapter = CreateConfiguredAdapter();

        // Act
        var orders = await adapter.PullOrdersAsync(DateTime.UtcNow.AddDays(-1));

        // Assert
        orders.Should().HaveCount(1);
        orders[0].PlatformOrderId.Should().Be("5001");
        orders[0].OrderNumber.Should().Be("#1001");
        orders[0].Status.Should().Be("paid");
        orders[0].TotalAmount.Should().Be(199.99m);
        orders[0].Currency.Should().Be("USD");
        orders[0].CustomerName.Should().Be("John Doe");
        orders[0].CustomerEmail.Should().Be("john@example.com");
        orders[0].Lines.Should().HaveCount(1);
        orders[0].Lines[0].SKU.Should().Be("SKU-001");
        orders[0].Lines[0].Quantity.Should().Be(2);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 12. PullOrders — No orders returns empty list
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullOrders_NoOrders_ReturnsEmpty()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/orders.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""orders"":[]}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 13. VerifyWebhookSignature — Valid HMAC returns true
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void VerifyWebhookSignature_ValidHmac_ReturnsTrue()
    {
        // Arrange
        const string secret = "test-webhook-secret";
        var body = Encoding.UTF8.GetBytes(@"{""id"":5001,""financial_status"":""paid""}");

        // Compute the expected HMAC-SHA256 base64
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(body);
        var expectedSignature = Convert.ToBase64String(hash);

        var adapter = CreateConfiguredAdapter(webhookSecret: secret);

        // Act
        var isValid = adapter.VerifyWebhookSignature(body, expectedSignature);

        // Assert
        isValid.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 14. VerifyWebhookSignature — Invalid HMAC returns false
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void VerifyWebhookSignature_InvalidHmac_ReturnsFalse()
    {
        // Arrange
        var body = Encoding.UTF8.GetBytes(@"{""id"":5001,""financial_status"":""paid""}");
        const string wrongSignature = "dGhpcyBpcyBub3QgdGhlIHJpZ2h0IHNpZ25hdHVyZQ==";

        var adapter = CreateConfiguredAdapter(webhookSecret: "test-webhook-secret");

        // Act
        var isValid = adapter.VerifyWebhookSignature(body, wrongSignature);

        // Assert
        isValid.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 15. RegisterWebhook — Valid topic, returns true
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RegisterWebhook_ValidTopic_ReturnsTrue()
    {
        // Arrange — stub webhooks.json POST for all 3 topics (orders/create, orders/updated, orders/cancelled)
        _mockServer
            .Given(Request.Create()
                .WithPath($"{AdminApiBase}/webhooks.json")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""webhook"":{""id"":1,""topic"":""orders/create"",
                    ""address"":""https://myapp.example.com/webhooks/shopify""}}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.RegisterWebhookAsync("https://myapp.example.com/webhooks/shopify");

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 16. VerifyWebhookSignature — No secret configured returns false
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void VerifyWebhookSignature_NoSecretConfigured_ReturnsFalse()
    {
        // Arrange — adapter with no webhook secret
        var options = Options.Create(new ShopifyOptions
        {
            ShopDomain = WireMockHostPort,
            AccessToken = TestAccessToken,
            LocationId = TestLocationId,
            WebhookSecret = string.Empty // no secret
        });
        var adapter = new ShopifyAdapter(CreateHttpClient(), _logger, options);
        var body = Encoding.UTF8.GetBytes(@"{""id"":1}");

        // Act
        var isValid = adapter.VerifyWebhookSignature(body, "any-signature");

        // Assert
        isValid.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 17. SupportsStockUpdate and SupportsPriceUpdate flags
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SupportsStockUpdate_IsTrue()
    {
        var adapter = CreateAdapter();
        adapter.SupportsStockUpdate.Should().BeTrue();
    }

    [Fact]
    public void SupportsPriceUpdate_IsTrue()
    {
        var adapter = CreateAdapter();
        adapter.SupportsPriceUpdate.Should().BeTrue();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test Infrastructure
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// DelegatingHandler that rewrites https:// → http:// in request URIs.
/// Allows testing adapters that hardcode HTTPS URLs against WireMock's HTTP server.
/// </summary>
internal sealed class HttpsToHttpRedirectHandler : DelegatingHandler
{
    public HttpsToHttpRedirectHandler(HttpMessageHandler inner) : base(inner) { }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is { Scheme: "https" })
        {
            var builder = new UriBuilder(request.RequestUri)
            {
                Scheme = Uri.UriSchemeHttp,
                Port = request.RequestUri.Port
            };
            request.RequestUri = builder.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
