using System.Net.Http;
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
/// ZalandoAdapter contract tests with WireMock.
/// Tests cover PlatformCode, TestConnection (OAuth2 token + article probe),
/// PullProducts (pagination), PushStockUpdate, PushPriceUpdate (EUR),
/// PullOrders, UpdateOrderStatus, TokenRefresh, and order field mapping.
///
/// ZalandoAdapter hardcodes https://auth.zalando.com and https://api.zalando.com.
/// Tests use HttpsToHttpRedirectHandler (same pattern as ShopifyAdapterTests) to
/// redirect those HTTPS URLs to WireMock's plain HTTP server.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Zalando")]
public class ZalandoAdapterTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<ZalandoAdapter> _logger;

    // WireMock authority: e.g. "127.0.0.1:PORT"
    private string WireMockAuthority => new Uri(_fixture.BaseUrl).Authority;

    private const string TestClientId = "test-client-id";
    private const string TestClientSecret = "test-client-secret";
    private const string TestAccessToken = "zalando-bearer-token-abc";

    // ─────────────────────────────────────────────────────────────────────────
    // Token endpoint: POST /oauth2/access_token  (auth.zalando.com)
    // Article probe:  GET  /partner/articles     (api.zalando.com)
    // ─────────────────────────────────────────────────────────────────────────
    private const string TokenPath = "/oauth2/access_token";
    private const string ArticlesPath = "/partner/articles";
    private const string InventoryPath = "/partner/inventory";
    private const string PricesPath = "/partner/prices";
    private const string OrdersPath = "/partner/orders";

    public ZalandoAdapterTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<ZalandoAdapter>();
    }

    public void Dispose()
    {
        // WireMockFixture handles server lifecycle
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an HttpClient that redirects https:// → http:// so that the adapter's
    /// hardcoded HTTPS base URLs are served by WireMock's plain HTTP server.
    /// </summary>
    private HttpClient CreateHttpClient()
    {
        var wireMockPort = new Uri(_fixture.BaseUrl).Port;
        var handler = new ZalandoHttpsToHttpHandler(new HttpClientHandler(), wireMockPort);
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>
    /// Creates an unconfigured ZalandoAdapter (no credentials seeded).
    /// </summary>
    private ZalandoAdapter CreateAdapter()
    {
        return new ZalandoAdapter(CreateHttpClient(), _logger);
    }

    /// <summary>
    /// Creates a ZalandoAdapter fully seeded with credentials via IOptions,
    /// bypassing TestConnectionAsync so tests can directly call API methods.
    /// </summary>
    private ZalandoAdapter CreateConfiguredAdapter()
    {
        var options = Options.Create(new ZalandoOptions
        {
            ClientId = TestClientId,
            ClientSecret = TestClientSecret,
            Enabled = true
        });
        return new ZalandoAdapter(CreateHttpClient(), _logger, options);
    }

    private Dictionary<string, string> ValidCredentials => new()
    {
        ["ClientId"] = TestClientId,
        ["ClientSecret"] = TestClientSecret
    };

    /// <summary>
    /// Stubs the OAuth2 token endpoint to return a valid bearer token.
    /// </summary>
    private void StubTokenEndpoint(string token = TestAccessToken, int expiresIn = 3600)
    {
        _mockServer
            .Given(Request.Create()
                .WithPath(TokenPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""access_token"":""{token}"",""token_type"":""Bearer"",""expires_in"":{expiresIn}}}"));
    }

    /// <summary>
    /// Stubs the article probe endpoint used by TestConnectionAsync.
    /// </summary>
    private void StubArticleProbe(int statusCode = 200)
    {
        _mockServer
            .Given(Request.Create()
                .WithPath(ArticlesPath)
                .WithParam("page", "0")
                .WithParam("pageSize", "1")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""content"":[],""totalElements"":0}"));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 1. PlatformCode_IsZalando
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PlatformCode_IsZalando()
    {
        var adapter = CreateAdapter();
        adapter.PlatformCode.Should().Be("Zalando");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. TestConnection_ValidToken_ReturnsSuccess
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TestConnection_ValidToken_ReturnsSuccess()
    {
        // Arrange
        StubTokenEndpoint();
        StubArticleProbe(200);

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Zalando");
        result.StoreName.Should().Be("Zalando Partner (OAuth2 OK)");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. TestConnection_NetworkError_ReturnsFailure
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TestConnection_NetworkError_ReturnsFailure()
    {
        // Arrange — token endpoint returns 401
        _mockServer
            .Given(Request.Create()
                .WithPath(TokenPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""invalid_client""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 4. PullProducts_ReturnsMappedArticles
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProducts_ReturnsMappedArticles()
    {
        // Arrange
        StubTokenEndpoint();

        var articlesJson = @"{
            ""content"":[
                {
                    ""sku"":""ZLD-001"",
                    ""name"":""Zalando Sneaker"",
                    ""availableUnits"":25,
                    ""price"":{""amount"":""89.99"",""currency"":""EUR""}
                }
            ],
            ""totalElements"":1
        }";

        _mockServer
            .Given(Request.Create()
                .WithPath(ArticlesPath)
                .WithParam("page", "0")
                .WithParam("pageSize", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(articlesJson));

        var adapter = CreateConfiguredAdapter();

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(1);
        products[0].SKU.Should().Be("ZLD-001");
        products[0].Name.Should().Be("Zalando Sneaker");
        products[0].Stock.Should().Be(25);
        products[0].SalePrice.Should().Be(89.99m);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 5. PullProducts_EmptyCatalog_ReturnsEmpty
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProducts_EmptyCatalog_ReturnsEmpty()
    {
        // Arrange
        StubTokenEndpoint();

        _mockServer
            .Given(Request.Create()
                .WithPath(ArticlesPath)
                .WithParam("page", "0")
                .WithParam("pageSize", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""content"":[],""totalElements"":0}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 6. PullProducts_Pagination_MultiplePages
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProducts_Pagination_MultiplePages()
    {
        // Arrange — page 0 returns 50 items (full page), page 1 returns 1 item
        StubTokenEndpoint();

        // Build 50 identical skeleton articles for page 0
        var items = string.Join(",", Enumerable.Range(1, 50).Select(i =>
            $@"{{""sku"":""SKU-{i:D3}"",""name"":""Product {i}"",""availableUnits"":10,""price"":{{""amount"":""9.99"",""currency"":""EUR""}}}}"));

        _mockServer
            .Given(Request.Create()
                .WithPath(ArticlesPath)
                .WithParam("page", "0")
                .WithParam("pageSize", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""content"":[{items}],""totalElements"":51}}"));

        _mockServer
            .Given(Request.Create()
                .WithPath(ArticlesPath)
                .WithParam("page", "1")
                .WithParam("pageSize", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""content"":[{""sku"":""SKU-051"",""name"":""Product 51"",""availableUnits"":3,""price"":{""amount"":""19.99"",""currency"":""EUR""}}],""totalElements"":51}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — both pages combined
        products.Should().HaveCount(51);
        products.Any(p => p.SKU == "SKU-001").Should().BeTrue();
        products.Any(p => p.SKU == "SKU-051").Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 7. PushStockUpdate_ValidEan_ReturnsTrue
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushStockUpdate_ValidEan_ReturnsTrue()
    {
        // Arrange
        StubTokenEndpoint();

        _mockServer
            .Given(Request.Create()
                .WithPath(InventoryPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""status"":""OK""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 42);

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 8. PushStockUpdate_HttpError_ReturnsFalse
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushStockUpdate_HttpError_ReturnsFalse()
    {
        // Arrange
        StubTokenEndpoint();

        _mockServer
            .Given(Request.Create()
                .WithPath(InventoryPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""message"":""Invalid SKU""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 10);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 9. PushPriceUpdate_ValidPrice_EurCurrency
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushPriceUpdate_ValidPrice_EurCurrency()
    {
        // Arrange
        StubTokenEndpoint();

        _mockServer
            .Given(Request.Create()
                .WithPath(PricesPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""status"":""OK""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.PushPriceUpdateAsync(Guid.NewGuid(), 129.99m);

        // Assert
        result.Should().BeTrue();

        // Verify the price request was actually sent to /partner/prices
        var calls = _mockServer.LogEntries
            .Where(e => e.RequestMessage.Path == PricesPath)
            .ToList();
        calls.Should().HaveCount(1);

        // Body must contain "EUR" currency
        var bodyText = calls[0].RequestMessage.Body ?? string.Empty;
        bodyText.Should().Contain("EUR");
        bodyText.Should().Contain("129.99");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 10. PushPriceUpdate_HttpError_ReturnsFalse
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushPriceUpdate_HttpError_ReturnsFalse()
    {
        // Arrange
        StubTokenEndpoint();

        _mockServer
            .Given(Request.Create()
                .WithPath(PricesPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(422)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""message"":""Price below minimum threshold""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.PushPriceUpdateAsync(Guid.NewGuid(), 0.01m);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 11. PullOrders_ReturnsOrders
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullOrders_ReturnsOrders()
    {
        // Arrange
        StubTokenEndpoint();

        var ordersJson = @"{
            ""content"":[{
                ""orderId"":""ZLD-ORD-9001"",
                ""orderNumber"":""Z9001"",
                ""status"":""PROCESSING"",
                ""createdAt"":""2026-02-01T08:00:00Z"",
                ""modifiedAt"":""2026-02-01T09:00:00Z"",
                ""totalAmount"":{""amount"":""149.90"",""currency"":""EUR""},
                ""shippingCost"":{""amount"":""4.90"",""currency"":""EUR""},
                ""customer"":{""firstName"":""Hans"",""lastName"":""Mueller"",""email"":""hans@example.de""},
                ""deliveryAddress"":{""addressLine1"":""Bahnhofstr. 12"",""city"":""Berlin""},
                ""orderItems"":[{
                    ""lineItemId"":""LI-001"",
                    ""sku"":""ZLD-001"",
                    ""name"":""Zalando Sneaker"",
                    ""quantity"":1,
                    ""unitPrice"":{""amount"":""89.99"",""currency"":""EUR""}
                }]
            }],
            ""totalElements"":1
        }";

        _mockServer
            .Given(Request.Create()
                .WithPath(OrdersPath)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(ordersJson));

        var adapter = CreateConfiguredAdapter();

        // Act
        var orders = await adapter.PullOrdersAsync(DateTime.UtcNow.AddDays(-7));

        // Assert
        orders.Should().HaveCount(1);
        orders[0].PlatformOrderId.Should().Be("ZLD-ORD-9001");
        orders[0].OrderNumber.Should().Be("Z9001");
        orders[0].Status.Should().Be("PROCESSING");
        orders[0].TotalAmount.Should().Be(149.90m);
        orders[0].ShippingCost.Should().Be(4.90m);
        orders[0].Currency.Should().Be("EUR");
        orders[0].CustomerName.Should().Be("Hans Mueller");
        orders[0].CustomerEmail.Should().Be("hans@example.de");
        orders[0].CustomerCity.Should().Be("Berlin");
        orders[0].Lines.Should().HaveCount(1);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 12. PullOrders_NoOrders_ReturnsEmpty
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullOrders_NoOrders_ReturnsEmpty()
    {
        // Arrange
        StubTokenEndpoint();

        _mockServer
            .Given(Request.Create()
                .WithPath(OrdersPath)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""content"":[],""totalElements"":0}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 13. TokenRefresh_ExpiredToken_Refreshes
    //     Each fresh ZalandoAdapter instance has no cached token and must
    //     request a new one before any API call.  Two independent instances
    //     should each hit the token endpoint exactly once.
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TokenRefresh_ExpiredToken_Refreshes()
    {
        // Arrange — token endpoint responds to any number of POST calls
        StubTokenEndpoint();
        StubArticleProbe(200);

        // First adapter — token fetched on first API call
        var adapter1 = CreateConfiguredAdapter();
        await adapter1.TestConnectionAsync(ValidCredentials);

        // Second adapter (fresh instance, no cached token) — token must be fetched again
        var adapter2 = CreateConfiguredAdapter();
        await adapter2.TestConnectionAsync(ValidCredentials);

        // Assert — WireMock received at least 2 POST requests to the token endpoint
        var tokenCalls = _mockServer.LogEntries
            .Where(e => e.RequestMessage.Path == TokenPath &&
                        e.RequestMessage.Method == "POST")
            .ToList();

        tokenCalls.Should().HaveCountGreaterThanOrEqualTo(2,
            "each fresh adapter instance must obtain its own token");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 14. OrderMapping_ContainsExpectedFields
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task OrderMapping_ContainsExpectedFields()
    {
        // Arrange
        StubTokenEndpoint();

        var ordersJson = @"{
            ""content"":[{
                ""orderId"":""ZLD-MAP-001"",
                ""orderNumber"":""ZMAP001"",
                ""status"":""SHIPPED"",
                ""createdAt"":""2026-01-20T12:30:00Z"",
                ""modifiedAt"":""2026-01-21T08:00:00Z"",
                ""totalAmount"":{""amount"":""249.00"",""currency"":""EUR""},
                ""shippingCost"":{""amount"":""0.00"",""currency"":""EUR""},
                ""customer"":{""firstName"":""Anna"",""lastName"":""Schmidt"",""email"":""anna@example.de""},
                ""deliveryAddress"":{""addressLine1"":""Hauptstr. 5"",""city"":""Munich""},
                ""orderItems"":[
                    {
                        ""lineItemId"":""LI-A"",
                        ""sku"":""ZLD-SHOE-42"",
                        ""name"":""Running Shoe"",
                        ""quantity"":2,
                        ""unitPrice"":{""amount"":""124.50"",""currency"":""EUR""}
                    },
                    {
                        ""lineItemId"":""LI-B"",
                        ""sku"":""ZLD-SOCK-M"",
                        ""name"":""Sport Socks"",
                        ""quantity"":3,
                        ""unitPrice"":{""amount"":""0.00"",""currency"":""EUR""}
                    }
                ]
            }],
            ""totalElements"":1
        }";

        _mockServer
            .Given(Request.Create()
                .WithPath(OrdersPath)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(ordersJson));

        var adapter = CreateConfiguredAdapter();

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert — full mapping check
        orders.Should().HaveCount(1);
        var order = orders[0];

        order.PlatformCode.Should().Be("Zalando");
        order.PlatformOrderId.Should().Be("ZLD-MAP-001");
        order.OrderNumber.Should().Be("ZMAP001");
        order.Status.Should().Be("SHIPPED");
        order.Currency.Should().Be("EUR");
        order.TotalAmount.Should().Be(249.00m);
        order.ShippingCost.Should().Be(0.00m);
        order.CustomerName.Should().Be("Anna Schmidt");
        order.CustomerEmail.Should().Be("anna@example.de");
        order.CustomerCity.Should().Be("Munich");
        order.OrderDate.Should().BeCloseTo(new DateTime(2026, 1, 20, 12, 30, 0, DateTimeKind.Utc), TimeSpan.FromSeconds(1));
        order.LastModifiedDate.Should().NotBeNull();

        order.Lines.Should().HaveCount(2);
        order.Lines[0].SKU.Should().Be("ZLD-SHOE-42");
        order.Lines[0].ProductName.Should().Be("Running Shoe");
        order.Lines[0].Quantity.Should().Be(2);
        order.Lines[0].UnitPrice.Should().Be(124.50m);
        order.Lines[0].LineTotal.Should().Be(249.00m);

        order.Lines[1].SKU.Should().Be("ZLD-SOCK-M");
        order.Lines[1].Quantity.Should().Be(3);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 15. UpdateOrderStatus_Success
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateOrderStatus_Success()
    {
        // Arrange
        StubTokenEndpoint();

        var orderId = "ZLD-ORD-7777";
        var encodedId = Uri.EscapeDataString(orderId);

        _mockServer
            .Given(Request.Create()
                .WithPath($"{OrdersPath}/{encodedId}/status")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""status"":""SHIPPED""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.UpdateOrderStatusAsync(orderId, "SHIPPED");

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 16. UpdateOrderStatus_HttpError_ReturnsFalse
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateOrderStatus_HttpError_ReturnsFalse()
    {
        // Arrange
        StubTokenEndpoint();

        var orderId = "ZLD-ORD-INVALID";
        var encodedId = Uri.EscapeDataString(orderId);

        _mockServer
            .Given(Request.Create()
                .WithPath($"{OrdersPath}/{encodedId}/status")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""message"":""Order not found""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.UpdateOrderStatusAsync(orderId, "SHIPPED");

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 17. UpdateOrderStatus_EmptyPackageId_ReturnsFalse (guard clause)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateOrderStatus_EmptyPackageId_ReturnsFalse()
    {
        // Arrange — no HTTP stub needed; adapter returns false before making any request
        StubTokenEndpoint();
        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.UpdateOrderStatusAsync(string.Empty, "SHIPPED");

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 18. SupportsStockUpdate and SupportsPriceUpdate flags
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

    // ══════════════════════════════════════════════════════════════════════════
    // 19. SupportsShipment_IsFalse (Zalando manages its own logistics)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SupportsShipment_IsFalse()
    {
        var adapter = CreateAdapter();
        adapter.SupportsShipment.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 20. GetCategories_ReturnsEmpty (Zalando manages taxonomy internally)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCategories_ReturnsEmpty()
    {
        var adapter = CreateAdapter();
        var categories = await adapter.GetCategoriesAsync();
        categories.Should().BeEmpty();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Test Infrastructure
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// DelegatingHandler that rewrites https:// → http:// in request URIs.
/// Allows testing adapters that hardcode HTTPS URLs (api.zalando.com, auth.zalando.com)
/// against WireMock's plain HTTP server without certificate concerns.
/// </summary>
internal sealed class ZalandoHttpsToHttpHandler : DelegatingHandler
{
    private readonly int _targetPort;

    public ZalandoHttpsToHttpHandler(HttpMessageHandler inner, int targetPort = -1) : base(inner)
    {
        _targetPort = targetPort;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is { Scheme: "https" })
        {
            var builder = new UriBuilder(request.RequestUri)
            {
                Scheme = Uri.UriSchemeHttp,
                Port = _targetPort > 0 ? _targetPort : request.RequestUri.Port
            };
            request.RequestUri = builder.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
