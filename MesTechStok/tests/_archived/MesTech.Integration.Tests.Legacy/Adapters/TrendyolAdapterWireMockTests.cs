using System.Net;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace MesTech.Integration.Tests.Adapters;

/// <summary>
/// TrendyolAdapter WireMock contract tests (A-M2-09).
/// Verifies HTTP client behavior without real Trendyol credentials.
/// WireMock replaces the live API — proves adapter mapping is correct.
/// </summary>
public class TrendyolAdapterWireMockTests : IClassFixture<WireMockFixture>
{
    private readonly WireMockFixture _fixture;

    public TrendyolAdapterWireMockTests(WireMockFixture fixture)
    {
        _fixture = fixture;
    }

    private TrendyolAdapter BuildAdapter()
    {
        var options = Options.Create(new TrendyolOptions
        {
            ProductionBaseUrl = _fixture.ServerUrl,
            SandboxBaseUrl = _fixture.ServerUrl,
            UseSandbox = false,
            Enabled = true
        });
        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<TrendyolAdapter>>().Object;
        return new TrendyolAdapter(httpClient, logger, options);
    }

    private Dictionary<string, string> BuildCredentials() => new()
    {
        ["ApiKey"] = "test-api-key",
        ["ApiSecret"] = "test-api-secret",
        ["SupplierId"] = "123456",
        ["BaseUrl"] = _fixture.ServerUrl
    };

    // ─────────────────────────────────────────────────────────────
    // Test 1: TestConnection succeeds with mocked product response
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        _fixture.Server.Reset();

        _fixture.Server
            .Given(Request.Create()
                .WithPath("/integration/product/sellers/123456/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"totalElements": 42, "totalPages": 1, "content": []}"""));

        var adapter = BuildAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(BuildCredentials());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Trendyol");
        result.ProductCount.Should().Be(42);
        result.StoreName.Should().Contain("123456");
    }

    // ─────────────────────────────────────────────────────────────
    // Test 2: PullProducts returns mapped products
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task PullProducts_ReturnsMappedProducts()
    {
        // Arrange
        _fixture.Server.Reset();

        // Single stub that handles both TestConnection (?size=1) and PullProducts (?size=50)
        // by returning the full product list for any GET to the products path.
        const string productsJson = """
            {
              "totalElements": 2,
              "totalPages": 1,
              "content": [
                {
                  "title": "Test Urun 1",
                  "stockCode": "SKU-T001",
                  "barcode": "8690000000001",
                  "salePrice": 99.90,
                  "listPrice": 129.90,
                  "quantity": 15,
                  "description": "Aciklama 1"
                },
                {
                  "title": "Test Urun 2",
                  "stockCode": "SKU-T002",
                  "barcode": "8690000000002",
                  "salePrice": 49.90,
                  "listPrice": 59.90,
                  "quantity": 0,
                  "description": "Aciklama 2"
                }
              ]
            }
            """;

        _fixture.Server
            .Given(Request.Create()
                .WithPath("/integration/product/sellers/123456/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(productsJson));

        var adapter = BuildAdapter();
        await adapter.TestConnectionAsync(BuildCredentials());

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().NotBeNull();
        products.Should().HaveCount(2);

        products[0].Name.Should().Be("Test Urun 1");
        products[0].SKU.Should().Be("SKU-T001");
        products[0].Barcode.Should().Be("8690000000001");
        products[0].SalePrice.Should().Be(99.90m);
        products[0].ListPrice.Should().Be(129.90m);
        products[0].Stock.Should().Be(15);

        products[1].Name.Should().Be("Test Urun 2");
        products[1].SKU.Should().Be("SKU-T002");
        products[1].Stock.Should().Be(0);
    }

    // ─────────────────────────────────────────────────────────────
    // Test 3: PingAsync returns true when server responds
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task PingAsync_ServerReachable_ReturnsTrue()
    {
        // Arrange
        _fixture.Server.Reset();

        _fixture.Server
            .Given(Request.Create()
                .WithPath("/")
                .UsingMethod("HEAD"))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK));

        var adapter = BuildAdapter();

        // Act
        var result = await adapter.PingAsync();

        // Assert
        result.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────
    // Test 4: TestConnection fails with missing credentials
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_MissingCredentials_ReturnsFailure()
    {
        // Arrange
        var adapter = BuildAdapter();
        var badCreds = new Dictionary<string, string>
        {
            ["ApiKey"] = "",
            ["ApiSecret"] = "",
            ["SupplierId"] = ""
        };

        // Act
        var result = await adapter.TestConnectionAsync(badCreds);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ─────────────────────────────────────────────────────────────
    // Test 5: PlatformCode is Trendyol
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void PlatformCode_IsTrendyol()
    {
        var adapter = BuildAdapter();

        adapter.PlatformCode.Should().Be("Trendyol");
        adapter.SupportsStockUpdate.Should().BeTrue();
        adapter.SupportsPriceUpdate.Should().BeTrue();
        adapter.SupportsShipment.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────
    // Test 6: GetCategories returns mapped categories
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCategories_ReturnsMappedList()
    {
        _fixture.Server.Reset();

        const string categoriesJson = """
            {"categories": [
                {"id": 1001, "name": "Elektronik", "parentId": null, "subCategories": []},
                {"id": 2001, "name": "Giyim", "parentId": null, "subCategories": []}
            ]}
            """;

        _fixture.Server
            .Given(Request.Create()
                .WithPath("/integration/product/product-categories")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(categoriesJson));

        var adapter = BuildAdapter();
        await adapter.TestConnectionAsync(BuildCredentials());

        var categories = await adapter.GetCategoriesAsync();

        categories.Should().NotBeNull();
        categories.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ─────────────────────────────────────────────────────────────
    // Test 7: PullOrders returns mapped orders
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task PullOrders_ReturnsMappedOrders()
    {
        _fixture.Server.Reset();

        const string ordersJson = """
            {"totalElements": 1, "totalPages": 1, "content": [
                {
                    "shipmentPackageId": 98765,
                    "orderNumber": "ORD-TR-001",
                    "orderDate": 1711900800000,
                    "status": "Created",
                    "totalPrice": 199.90,
                    "lines": [
                        {"productName": "Test Urun", "barcode": "8690000000001",
                         "quantity": 2, "amount": 199.90, "merchantSku": "SKU-T001"}
                    ],
                    "shipmentAddress": {"fullName": "Ali Veli", "city": "Istanbul"}
                }
            ]}
            """;

        // Stub both products (for TestConnection) and orders paths
        _fixture.Server
            .Given(Request.Create()
                .WithPath("/integration/product/sellers/123456/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody("""{"totalElements": 1, "totalPages": 1, "content": []}"""));

        _fixture.Server
            .Given(Request.Create()
                .WithPath("/integration/order/sellers/123456/orders")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(ordersJson));

        var adapter = BuildAdapter();
        await adapter.TestConnectionAsync(BuildCredentials());

        var orders = await adapter.PullOrdersAsync(DateTime.UtcNow.AddDays(-7));

        orders.Should().NotBeNull();
        orders.Should().HaveCount(1);
        orders[0].PlatformOrderNumber.Should().Be("ORD-TR-001");
    }

    // ─────────────────────────────────────────────────────────────
    // Test 8: API returns 401 → graceful error handling
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_Unauthorized_ReturnsFailure()
    {
        _fixture.Server.Reset();

        _fixture.Server
            .Given(Request.Create()
                .WithPath("/integration/product/sellers/123456/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized)
                .WithBody("""{"errors": [{"message": "Invalid API key"}]}"""));

        var adapter = BuildAdapter();
        var result = await adapter.TestConnectionAsync(BuildCredentials());

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────
    // Test 9: API returns 429 → rate limit handling
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_RateLimited_ReturnsFailure()
    {
        _fixture.Server.Reset();

        _fixture.Server
            .Given(Request.Create()
                .WithPath("/integration/product/sellers/123456/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.TooManyRequests)
                .WithHeader("Retry-After", "5")
                .WithBody("""{"errors": [{"message": "Too Many Requests"}]}"""));

        var adapter = BuildAdapter();
        var result = await adapter.TestConnectionAsync(BuildCredentials());

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────
    // Test 10: API returns 500 → server error handling
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_ServerError_ReturnsFailure()
    {
        _fixture.Server.Reset();

        _fixture.Server
            .Given(Request.Create()
                .WithPath("/integration/product/sellers/123456/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.InternalServerError));

        var adapter = BuildAdapter();
        var result = await adapter.TestConnectionAsync(BuildCredentials());

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────
    // Test 11: CheckHealth returns health status
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckHealth_ReturnsHealthDto()
    {
        _fixture.Server.Reset();

        _fixture.Server
            .Given(Request.Create()
                .WithPath("/integration/product/sellers/123456/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody("""{"totalElements": 10, "totalPages": 1, "content": []}"""));

        var adapter = BuildAdapter();
        await adapter.TestConnectionAsync(BuildCredentials());

        var health = await adapter.CheckHealthAsync();

        health.Should().NotBeNull();
        health.PlatformCode.Should().Be("Trendyol");
    }
}
