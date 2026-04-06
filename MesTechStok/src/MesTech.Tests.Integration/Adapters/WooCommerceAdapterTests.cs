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
/// WooCommerceAdapter contract tests with WireMock.
/// Tests cover PlatformCode, TestConnection, PullProducts (page-based pagination),
/// PushStockUpdate, PullOrders, UpdateOrderStatus, and capability flags.
///
/// WooCommerce uses Basic Auth (ConsumerKey:ConsumerSecret → Base64).
/// API prefix: /wp-json/wc/v3/
/// Adapter builds absolute URLs as {SiteUrl}/wp-json/wc/v3/..., so SiteUrl is
/// set directly to the WireMock BaseUrl (http://127.0.0.1:PORT).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "WooCommerce")]
public class WooCommerceAdapterTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<WooCommerceAdapter> _logger;

    private const string TestConsumerKey = "ck_test1234567890abcdef";
    private const string TestConsumerSecret = "cs_test1234567890abcdef";
    private const string WcApiBase = "/wp-json/wc/v3";

    public WooCommerceAdapterTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<WooCommerceAdapter>();
    }

    public void Dispose()
    {
        // WireMockFixture handles server lifecycle
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private HttpClient CreateHttpClient()
        => new HttpClient();

    private WooCommerceAdapter CreateAdapter()
        => new WooCommerceAdapter(CreateHttpClient(), _logger);

    /// <summary>
    /// Creates an adapter seeded from IOptions — SiteUrl points to WireMock BaseUrl.
    /// Credentials are baked in so PullProducts/PullOrders can be called directly.
    /// </summary>
    private WooCommerceAdapter CreateConfiguredAdapter()
    {
        var options = Options.Create(new WooCommerceOptions
        {
            SiteUrl = _fixture.BaseUrl,
            ConsumerKey = TestConsumerKey,
            ConsumerSecret = TestConsumerSecret,
            Enabled = true
        });
        return new WooCommerceAdapter(CreateHttpClient(), _logger, options);
    }

    private Dictionary<string, string> ValidCredentials => new()
    {
        ["SiteUrl"] = _fixture.BaseUrl,
        ["ConsumerKey"] = TestConsumerKey,
        ["ConsumerSecret"] = TestConsumerSecret
    };

    private void StubSystemStatus(string siteUrl = "https://mystore.example.com")
    {
        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/system_status")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""environment"":{{""site_url"":""{siteUrl}"",""wp_version"":""6.4""}}}}"));
    }

    private void StubProductsCountEndpoint(int total = 25)
    {
        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/products")
                .WithParam("per_page", "1")
                .WithParam("page", "1")
                .WithParam("status", "publish")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("X-WP-Total", total.ToString())
                .WithHeader("X-WP-TotalPages", "1")
                .WithBody(@"[{""id"":1,""name"":""Test"",""sku"":""T-001"",""price"":""10.00"",""stock_quantity"":5}]"));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 1. PlatformCode
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PlatformCode_WhenCalled_ReturnsWooCommerce()
    {
        var adapter = CreateAdapter();
        adapter.PlatformCode.Should().Be("WooCommerce");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. TestConnection — Valid site returns success
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TestConnection_WithValidSite_ReturnsSuccess()
    {
        // Arrange
        StubSystemStatus("https://mystore.example.com");
        StubProductsCountEndpoint(42);

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("WooCommerce");
        result.StoreName.Should().Be("https://mystore.example.com");
        result.ProductCount.Should().Be(42);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. TestConnection — Error response returns failure
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TestConnection_WithErrorResponse_ReturnsFailure()
    {
        // Arrange — system_status returns 401
        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/system_status")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""code"":""woocommerce_rest_cannot_view"",""message"":""Sorry, you cannot list resources.""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unauthorized");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 4. PullProducts — Returns mapped products
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProducts_Returns_Mapped()
    {
        // Arrange
        var productsJson = @"[
            {""id"":101,""name"":""Red Hoodie"",""sku"":""RH-001"",
             ""price"":""89.99"",""stock_quantity"":30,""manage_stock"":true,""status"":""publish""},
            {""id"":102,""name"":""Blue Jeans"",""sku"":""BJ-002"",
             ""price"":""129.99"",""stock_quantity"":15,""manage_stock"":true,""status"":""publish""}
        ]";

        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/products")
                .WithParam("per_page", "100")
                .WithParam("page", "1")
                .WithParam("status", "publish")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("X-WP-TotalPages", "1")
                .WithBody(productsJson));

        var adapter = CreateConfiguredAdapter();

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(2);
        products[0].Name.Should().Be("Red Hoodie");
        products[0].SKU.Should().Be("RH-001");
        products[0].SalePrice.Should().Be(89.99m);
        products[0].Stock.Should().Be(30);
        products[1].Name.Should().Be("Blue Jeans");
        products[1].SKU.Should().Be("BJ-002");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 5. PullProducts — Empty store returns empty list
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProducts_EmptyStore_ReturnsEmpty()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/products")
                .WithParam("per_page", "100")
                .WithParam("page", "1")
                .WithParam("status", "publish")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("X-WP-TotalPages", "1")
                .WithBody(@"[]"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 6. PullProducts — Pagination with multiple pages
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProducts_Pagination_MultiplePages()
    {
        // Arrange — 2 pages
        var page1Json = @"[{""id"":1,""name"":""Product P1"",""sku"":""P1-001"",
            ""price"":""10.00"",""stock_quantity"":5,""status"":""publish""}]";

        var page2Json = @"[{""id"":2,""name"":""Product P2"",""sku"":""P2-002"",
            ""price"":""20.00"",""stock_quantity"":8,""status"":""publish""}]";

        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/products")
                .WithParam("per_page", "100")
                .WithParam("page", "1")
                .WithParam("status", "publish")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("X-WP-TotalPages", "2") // 2 total pages
                .WithBody(page1Json));

        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/products")
                .WithParam("per_page", "100")
                .WithParam("page", "2")
                .WithParam("status", "publish")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("X-WP-TotalPages", "2")
                .WithBody(page2Json));

        var adapter = CreateConfiguredAdapter();

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — products from both pages
        products.Should().HaveCount(2);
        products.Select(p => p.SKU).Should().Contain(new[] { "P1-001", "P2-002" });
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 7. PushStockUpdate — Finds product, updates stock
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushStockUpdate_FindsProduct_Updates()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var sku = productId.ToString();
        const int wooProductId = 201;

        // Step 1: GET products?sku={sku}
        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/products")
                .WithParam("sku", sku)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"[{{""id"":{wooProductId},""name"":""Test Product"",""sku"":""{sku}"",
                    ""stock_quantity"":10,""manage_stock"":true}}]"));

        // Step 2: PUT products/{id}
        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/products/{wooProductId}")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""id"":{wooProductId},""stock_quantity"":75}}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 75);

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 8. PushStockUpdate — Product not found, returns false
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushStockUpdate_ProductNotFound_ReturnsFalse()
    {
        // Arrange — SKU search returns empty list
        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"[]"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 10);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 9. PullOrders — Processing status returns orders
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullOrders_Processing_ReturnsOrders()
    {
        // Arrange
        var ordersJson = @"[{
            ""id"":3001,
            ""number"":""3001"",
            ""status"":""processing"",
            ""date_created"":""2026-03-01T12:00:00"",
            ""currency"":""USD"",
            ""total"":""249.98"",
            ""discount_total"":""0.00"",
            ""billing"":{
                ""first_name"":""Jane"",
                ""last_name"":""Smith"",
                ""email"":""jane@example.com"",
                ""phone"":""+9876543210"",
                ""address_1"":""456 Oak Ave"",
                ""address_2"":"""",
                ""city"":""Chicago""
            },
            ""line_items"":[
                {""id"":4001,""name"":""Red Hoodie"",""sku"":""RH-001"",
                 ""quantity"":1,""price"":""89.99"",""total"":""89.99""},
                {""id"":4002,""name"":""Blue Jeans"",""sku"":""BJ-002"",
                 ""quantity"":1,""price"":""129.99"",""total"":""129.99""}
            ]
        }]";

        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/orders")
                .WithParam("status", "processing")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("X-WP-TotalPages", "1")
                .WithBody(ordersJson));

        var adapter = CreateConfiguredAdapter();

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().HaveCount(1);
        orders[0].PlatformOrderId.Should().Be("3001");
        orders[0].PlatformCode.Should().Be("WooCommerce");
        orders[0].Status.Should().Be("processing");
        orders[0].TotalAmount.Should().Be(249.98m);
        orders[0].Currency.Should().Be("USD");
        orders[0].CustomerName.Should().Be("Jane Smith");
        orders[0].CustomerEmail.Should().Be("jane@example.com");
        orders[0].CustomerCity.Should().Be("Chicago");
        orders[0].Lines.Should().HaveCount(2);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 10. PullOrders — No orders returns empty list
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullOrders_NoOrders_ReturnsEmpty()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/orders")
                .WithParam("status", "processing")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("X-WP-TotalPages", "1")
                .WithBody(@"[]"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 11. UpdateOrderStatus — Success
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateOrderStatus_Success()
    {
        // Arrange
        const string orderId = "3001";

        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/orders/{orderId}")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""id"":3001,""status"":""completed""}}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.UpdateOrderStatusAsync(orderId, "completed");

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 12. UpdateOrderStatus — Not found returns false
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateOrderStatus_NotFound_ReturnsFalse()
    {
        // Arrange
        const string orderId = "9999";

        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/orders/{orderId}")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""code"":""woocommerce_rest_order_invalid_id"",""message"":""Invalid ID.""}"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.UpdateOrderStatusAsync(orderId, "completed");

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 13. PullProducts — Price format parses correctly (decimal string)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PullProducts_PriceFormatParsesCorrectly()
    {
        // Arrange — price as decimal string (WooCommerce returns strings for prices)
        var productsJson = @"[
            {""id"":301,""name"":""Laptop Stand"",""sku"":""LS-001"",
             ""price"":""1299.50"",""stock_quantity"":7,""status"":""publish""}
        ]";

        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/products")
                .WithParam("per_page", "100")
                .WithParam("page", "1")
                .WithParam("status", "publish")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("X-WP-TotalPages", "1")
                .WithBody(productsJson));

        var adapter = CreateConfiguredAdapter();

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(1);
        products[0].SalePrice.Should().Be(1299.50m);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 14. GetCategories — Returns mapped categories from API
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCategories_ReturnsEmpty_WhenApiReturnsEmpty()
    {
        // Arrange — WooCommerce categories endpoint returns empty array
        _mockServer
            .Given(Request.Create()
                .WithPath($"{WcApiBase}/products/categories")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"[]"));

        var adapter = CreateConfiguredAdapter();

        // Act
        var categories = await adapter.GetCategoriesAsync();

        // Assert
        categories.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 15. SupportsStockUpdate flag is true
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SupportsStockUpdate_IsTrue()
    {
        var adapter = CreateAdapter();
        adapter.SupportsStockUpdate.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 16. SupportsPriceUpdate flag is true (implemented in Dalga 14)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SupportsPriceUpdate_IsTrue()
    {
        var adapter = CreateAdapter();
        adapter.SupportsPriceUpdate.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 17. PushPriceUpdate — Always returns false (stub)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PushPriceUpdate_Stub_ReturnsFalse()
    {
        // Arrange — no HTTP stub needed (stub returns false immediately)
        var adapter = CreateConfiguredAdapter();

        // Act
        var result = await adapter.PushPriceUpdateAsync(Guid.NewGuid(), 99.99m);

        // Assert
        result.Should().BeFalse();
    }
}
