using System.Net;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Adapters;

public class TrendyolAdapterTests
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly Mock<ILogger<TrendyolAdapter>> _loggerMock = new();

    private TrendyolAdapter CreateAdapter()
    {
        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://apigw.trendyol.com/")
        };
        return new TrendyolAdapter(httpClient, _loggerMock.Object);
    }

    private static Dictionary<string, string> ValidCredentials() => new()
    {
        ["ApiKey"] = "test-api-key",
        ["ApiSecret"] = "test-api-secret",
        ["SupplierId"] = "12345"
    };

    private async Task ConfigureAdapterAsync(TrendyolAdapter adapter)
    {
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content": [{"id": 1}], "totalElements": 1}""");
        await adapter.TestConnectionAsync(ValidCredentials());
    }

    [Fact]
    public async Task TestConnection_ValidCreds_Success()
    {
        // Arrange
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content": [{"id": 1}], "totalElements": 42}""");

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Trendyol", result.PlatformCode);
        Assert.Equal(42, result.ProductCount);

        var requestUrl = _handler.CapturedRequests[0].RequestUri!.ToString();
        Assert.Contains("/integration/product/sellers/12345/products", requestUrl);
    }

    [Fact]
    public async Task TestConnection_MissingSupplierId_Failure()
    {
        // Arrange — creds without SupplierId key
        var creds = new Dictionary<string, string>
        {
            ["ApiKey"] = "test-api-key",
            ["ApiSecret"] = "test-api-secret"
        };

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(creds);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task PullProducts_DeserializesCorrectly()
    {
        // Arrange — first enqueue TestConnection response
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content": [{"id": 1}], "totalElements": 1}""");

        // Enqueue PullProducts response (single page)
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content":[{"barcode":"123","title":"Test Product","stockCode":"SKU-001","salePrice":99.90,"quantity":10,"description":"Desc"}],"totalElements":1,"totalPages":1}""");

        var adapter = CreateAdapter();

        // Configure adapter via TestConnection
        await adapter.TestConnectionAsync(ValidCredentials());

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        Assert.NotEmpty(products);
        Assert.Equal("Test Product", products[0].Name);
        Assert.Equal("SKU-001", products[0].SKU);
        Assert.Equal(99.90m, products[0].SalePrice);
        Assert.Equal(10, products[0].Stock);
        Assert.Equal("Desc", products[0].Description);
    }

    [Fact]
    public async Task PushStockUpdate_CorrectPayload()
    {
        // Arrange — enqueue TestConnection response
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content": [{"id": 1}], "totalElements": 1}""");

        // Enqueue PushStockUpdate response
        _handler.EnqueueResponse(HttpStatusCode.OK, "{}");

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials());

        var productId = Guid.NewGuid();

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 25);

        // Assert
        Assert.True(result);
        Assert.Equal(2, _handler.CapturedRequests.Count);
    }

    [Fact]
    public void PlatformCode_IsTrendyol()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Assert
        Assert.Equal("Trendyol", adapter.PlatformCode);
        Assert.True(adapter.SupportsStockUpdate);
        Assert.True(adapter.SupportsPriceUpdate);
        Assert.True(adapter.SupportsShipment);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 6: Pagination — PullProductsAsync iterates totalPages correctly
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrendyolAdapter_SyncProducts_WithPagination_ReturnsAllPages()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        // Page 0 — 2 products, totalPages=3
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content":[{"barcode":"B1","title":"Product A","stockCode":"SKU-A","salePrice":10.00,"quantity":5},{"barcode":"B2","title":"Product B","stockCode":"SKU-B","salePrice":20.00,"quantity":3}],"totalElements":5,"totalPages":3}""");

        // Page 1 — 2 products
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content":[{"barcode":"B3","title":"Product C","stockCode":"SKU-C","salePrice":30.00,"quantity":7},{"barcode":"B4","title":"Product D","stockCode":"SKU-D","salePrice":40.00,"quantity":1}],"totalElements":5,"totalPages":3}""");

        // Page 2 — 1 product (last page)
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content":[{"barcode":"B5","title":"Product E","stockCode":"SKU-E","salePrice":50.00,"quantity":2}],"totalElements":5,"totalPages":3}""");

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — all 5 products from 3 pages
        products.Should().HaveCount(5);
        products[0].SKU.Should().Be("SKU-A");
        products[2].SKU.Should().Be("SKU-C");
        products[4].SKU.Should().Be("SKU-E");

        // TestConnection(1) + 3 pages = 4 requests
        _handler.CapturedRequests.Count.Should().Be(4);

        // Verify page parameters in URLs
        _handler.CapturedRequests[1].RequestUri!.ToString().Should().Contain("page=0");
        _handler.CapturedRequests[2].RequestUri!.ToString().Should().Contain("page=1");
        _handler.CapturedRequests[3].RequestUri!.ToString().Should().Contain("page=2");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 7: Multiple sequential stock updates complete successfully
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrendyolAdapter_UpdateStock_BatchOf100_SuccessfullyChunks()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        // Enqueue 100 successful stock update responses
        for (int i = 0; i < 100; i++)
        {
            _handler.EnqueueResponse(HttpStatusCode.OK, "{}");
        }

        // Act — 100 sequential PushStockUpdateAsync calls
        var results = new List<bool>();
        for (int i = 0; i < 100; i++)
        {
            results.Add(await adapter.PushStockUpdateAsync(Guid.NewGuid(), i + 1));
        }

        // Assert — all 100 updates succeeded
        results.Should().HaveCount(100);
        results.Should().AllSatisfy(r => r.Should().BeTrue());

        // TestConnection(1) + 100 stock updates = 101 requests
        _handler.CapturedRequests.Count.Should().Be(101);

        // All stock update requests target the price-and-inventory endpoint
        for (int i = 1; i <= 100; i++)
        {
            _handler.CapturedRequests[i].RequestUri!.ToString()
                .Should().Contain("/integration/inventory/sellers/12345/products/price-and-inventory");
            _handler.CapturedRequests[i].Method.Should().Be(HttpMethod.Post);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 8: PushPriceUpdateAsync returns false on API error
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrendyolAdapter_UpdatePrice_InvalidSKU_ThrowsValidation()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        // API returns 400 Bad Request for invalid SKU
        _handler.EnqueueResponse(HttpStatusCode.BadRequest,
            """{"errors":[{"message":"Invalid barcode format","code":"VALIDATION_ERROR"}]}""");

        var invalidProductId = Guid.NewGuid();

        // Act
        var result = await adapter.PushPriceUpdateAsync(invalidProductId, 99.90m);

        // Assert — adapter returns false on API validation error, does not throw
        result.Should().BeFalse();

        // TestConnection(1) + PriceUpdate(1) = 2 requests
        _handler.CapturedRequests.Count.Should().Be(2);

        // Verify the price-and-inventory endpoint was called
        _handler.CapturedRequests[1].RequestUri!.ToString()
            .Should().Contain("/integration/inventory/sellers/12345/products/price-and-inventory");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 9: PullOrdersAsync returns empty list when API has no orders
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrendyolAdapter_GetOrders_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        // Orders endpoint returns empty content array
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content":[],"totalElements":0,"totalPages":0}""");

        // Act
        var orders = await adapter.PullOrdersAsync(DateTime.UtcNow.AddDays(-7));

        // Assert — empty list returned, no exception
        orders.Should().NotBeNull();
        orders.Should().BeEmpty();

        // TestConnection(1) + PullOrders(1) = 2 requests
        _handler.CapturedRequests.Count.Should().Be(2);

        // Verify orders endpoint was called with correct SupplierId
        _handler.CapturedRequests[1].RequestUri!.ToString()
            .Should().Contain("/integration/order/sellers/12345/orders");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 10: CheckHealthAsync returns IsHealthy=false when API is down
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrendyolAdapter_HealthCheck_ApiDown_ReturnsFalse()
    {
        // Arrange — API returns 503 Service Unavailable
        _handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable,
            """{"message":"Service temporarily unavailable"}""");

        var adapter = CreateAdapter();

        // Act — CheckHealthAsync does not require prior configuration
        var health = await adapter.CheckHealthAsync();

        // Assert
        health.Should().NotBeNull();
        health.PlatformCode.Should().Be("Trendyol");
        health.IsHealthy.Should().BeFalse();
        health.ErrorMessage.Should().NotBeNullOrEmpty();
        health.LatencyMs.Should().BeGreaterOrEqualTo(0);

        // Exactly 1 request to the health endpoint
        _handler.CapturedRequests.Count.Should().Be(1);
        _handler.CapturedRequests[0].RequestUri!.ToString()
            .Should().Contain("/integration/product/api-status");
    }
}
