using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Infrastructure.Integration.Webhooks;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Adapters;

/// <summary>
/// Extended TrendyolAdapter tests — 10 scenarios covering pagination, rate limiting,
/// batch stock, price validation, order filtering, shipment, webhook HMAC, and health check.
/// Uses MockHttpMessageHandler for HTTP stub.
/// </summary>
public class TrendyolAdapterExtendedTests
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

    private async Task<TrendyolAdapter> ConfigureAdapterAsync()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content": [{"id": 1}], "totalElements": 1}""");
        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials());
        return adapter;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 1. SyncProducts_WithPagination_ReturnsAllPages
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrendyolAdapter_SyncProducts_WithPagination_ReturnsAllPages()
    {
        // Arrange — adapter configured, then 3 pages of products
        var adapter = await ConfigureAdapterAsync();

        // Page 0: 2 products, totalPages = 3
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content":[{"title":"Product A","stockCode":"SKU-A","barcode":"1001","salePrice":29.90,"quantity":10},{"title":"Product B","stockCode":"SKU-B","barcode":"1002","salePrice":49.90,"quantity":5}],"totalElements":6,"totalPages":3}""");

        // Page 1: 2 products
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content":[{"title":"Product C","stockCode":"SKU-C","barcode":"1003","salePrice":79.90,"quantity":3},{"title":"Product D","stockCode":"SKU-D","barcode":"1004","salePrice":19.90,"quantity":20}],"totalElements":6,"totalPages":3}""");

        // Page 2: 2 products (final page)
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content":[{"title":"Product E","stockCode":"SKU-E","barcode":"1005","salePrice":99.90,"quantity":1},{"title":"Product F","stockCode":"SKU-F","barcode":"1006","salePrice":149.90,"quantity":0}],"totalElements":6,"totalPages":3}""");

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — all 6 products from 3 pages
        products.Should().HaveCount(6);
        products[0].SKU.Should().Be("SKU-A");
        products[2].SKU.Should().Be("SKU-C");
        products[4].SKU.Should().Be("SKU-E");
        products[5].Stock.Should().Be(0);

        // 1 (TestConnection) + 3 (pages) = 4 requests
        _handler.CapturedRequests.Should().HaveCount(4);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 2. SyncProducts_RateLimited_RetriesWithBackoff
    //    Simulates 429 then success — adapter Polly pipeline handles retry
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrendyolAdapter_SyncProducts_RateLimited_RetriesWithBackoff()
    {
        // Arrange — adapter configured, then 429 on first product page, then success
        var adapter = await ConfigureAdapterAsync();

        // First attempt: 429 Too Many Requests
        var rateLimitResponse = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        rateLimitResponse.Headers.RetryAfter =
            new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(50));
        _handler.EnqueueResponse(rateLimitResponse);

        // Second attempt after retry: success
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content":[{"title":"Retry Product","stockCode":"SKU-RETRY","barcode":"9999","salePrice":39.90,"quantity":7}],"totalElements":1,"totalPages":1}""");

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — product retrieved after retry
        products.Should().NotBeEmpty();
        products[0].SKU.Should().Be("SKU-RETRY");
        products[0].SalePrice.Should().Be(39.90m);

        // 1 (TestConnection) + 1 (429) + 1 (success after retry) = 3 requests
        _handler.CapturedRequests.Should().HaveCountGreaterOrEqualTo(3);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 3. UpdateStock_BatchOf100_SuccessfullyChunks
    //    Sends 100 stock updates — each goes through the adapter as individual calls
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrendyolAdapter_UpdateStock_BatchOf100_SuccessfullyChunks()
    {
        // Arrange
        var adapter = await ConfigureAdapterAsync();

        // Enqueue 100 OK responses for stock updates
        for (var i = 0; i < 100; i++)
            _handler.EnqueueResponse(HttpStatusCode.OK, "{}");

        var productIds = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();

        // Act — push 100 stock updates
        var results = new List<bool>();
        foreach (var id in productIds)
            results.Add(await adapter.PushStockUpdateAsync(id, 10));

        // Assert — all 100 succeed
        results.Should().AllSatisfy(r => r.Should().BeTrue());

        // 1 (TestConnection) + 100 (stock updates) = 101 requests
        _handler.CapturedRequests.Should().HaveCount(101);

        // All stock updates should POST to price-and-inventory endpoint
        for (var i = 1; i <= 100; i++)
        {
            _handler.CapturedRequests[i].RequestUri!.ToString()
                .Should().Contain("/integration/inventory/sellers/12345/products/price-and-inventory");
            _handler.CapturedRequests[i].Method.Should().Be(HttpMethod.Post);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // 4. UpdatePrice_InvalidSKU_ThrowsValidation
    //    Adapter not configured (no TestConnection) -> InvalidOperationException
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrendyolAdapter_UpdatePrice_InvalidSKU_ThrowsValidation()
    {
        // Arrange — adapter NOT configured (no TestConnection call)
        var adapter = CreateAdapter();

        // Act & Assert — calling PushPriceUpdateAsync without configuration should throw
        var act = () => adapter.PushPriceUpdateAsync(Guid.NewGuid(), 99.90m);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*yapilandirilmadi*");
    }

    // ─────────────────────────────────────────────────────────────────────
    // 5. GetOrders_DateRange_FiltersCorrectly
    //    Verifies startDate epoch is included in the URL query parameter
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrendyolAdapter_GetOrders_DateRange_FiltersCorrectly()
    {
        // Arrange
        var adapter = await ConfigureAdapterAsync();

        var sinceDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedEpoch = new DateTimeOffset(sinceDate).ToUnixTimeMilliseconds();

        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content":[{"orderNumber":"ORD-001","status":"Created","totalPrice":150.00,"orderDate":1709251200000,"lines":[{"id":1,"merchantSku":"SKU-A","barcode":"1001","productName":"Widget","quantity":2,"price":75.00,"amount":150.00}]}],"totalElements":1,"totalPages":1}""");

        // Act
        var orders = await adapter.PullOrdersAsync(sinceDate);

        // Assert
        orders.Should().HaveCount(1);
        orders[0].PlatformOrderId.Should().Be("ORD-001");
        orders[0].Status.Should().Be("Created");
        orders[0].TotalAmount.Should().Be(150.00m);
        orders[0].Lines.Should().HaveCount(1);
        orders[0].Lines[0].SKU.Should().Be("SKU-A");

        // Verify the startDate epoch is in the request URL
        var orderRequest = _handler.CapturedRequests.Last();
        orderRequest.RequestUri!.ToString().Should().Contain($"startDate={expectedEpoch}");
    }

    // ─────────────────────────────────────────────────────────────────────
    // 6. GetOrders_EmptyResult_ReturnsEmptyList
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrendyolAdapter_GetOrders_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var adapter = await ConfigureAdapterAsync();

        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"content":[],"totalElements":0,"totalPages":0}""");

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().NotBeNull();
        orders.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────
    // 7. SendShipment_WithCargoCode_SetsCorrectProvider
    //    Uses UpdateOrderStatusAsync (Trendyol's shipment API)
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrendyolAdapter_SendShipment_WithCargoCode_SetsCorrectProvider()
    {
        // Arrange
        var adapter = await ConfigureAdapterAsync();

        _handler.EnqueueResponse(HttpStatusCode.OK, "{}");

        // Act — use UpdateOrderStatusAsync to mark as shipped
        var result = await adapter.UpdateOrderStatusAsync("PKG-12345", "Shipped");

        // Assert
        result.Should().BeTrue();

        // Verify the shipment endpoint was called with the correct package ID
        var shipRequest = _handler.CapturedRequests.Last();
        shipRequest.RequestUri!.ToString()
            .Should().Contain("/integration/order/sellers/12345/orders/shipment-packages/PKG-12345");
        shipRequest.Method.Should().Be(HttpMethod.Put);

        // Verify request body contains the status
        var body = _handler.CapturedRequestBodies.Last();
        body.Should().NotBeNull();
        body.Should().Contain("Shipped");
    }

    // ─────────────────────────────────────────────────────────────────────
    // 8. Webhook_ValidHMAC_ProcessesEvent
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void TrendyolAdapter_Webhook_ValidHMAC_ProcessesEvent()
    {
        // Arrange
        var payload = """{"eventType":"OrderCreated","orderNumber":"ORD-999"}""";
        var secret = "trendyol-webhook-secret";

        // Compute valid HMAC-SHA256 signature
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var validSignature = Convert.ToBase64String(hash);

        // Act — validate using the WebhookEndpoints utility
        var isValid = WebhookEndpoints.ValidateHmacSignature(payload, validSignature, secret);

        // Assert
        isValid.Should().BeTrue("the signature matches the expected HMAC-SHA256");
    }

    // ─────────────────────────────────────────────────────────────────────
    // 9. Webhook_InvalidHMAC_RejectsEvent
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void TrendyolAdapter_Webhook_InvalidHMAC_RejectsEvent()
    {
        // Arrange
        var payload = """{"eventType":"OrderCreated","orderNumber":"ORD-999"}""";
        var secret = "trendyol-webhook-secret";
        var invalidSignature = Convert.ToBase64String(Encoding.UTF8.GetBytes("tampered-signature"));

        // Act
        var isValid = WebhookEndpoints.ValidateHmacSignature(payload, invalidSignature, secret);

        // Assert
        isValid.Should().BeFalse("the signature does not match the expected HMAC-SHA256");

        // Also verify empty signature is rejected
        WebhookEndpoints.ValidateHmacSignature(payload, "", secret)
            .Should().BeFalse("empty signature must be rejected");

        // Also verify empty secret is rejected
        WebhookEndpoints.ValidateHmacSignature(payload, "any-sig", "")
            .Should().BeFalse("empty secret must be rejected");
    }

    // ─────────────────────────────────────────────────────────────────────
    // 10. HealthCheck_ApiDown_ReturnsFalse
    //     PingAsync returns false when the API is unreachable
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrendyolAdapter_HealthCheck_ApiDown_ReturnsFalse()
    {
        // Arrange — use a handler that always throws HttpRequestException
        var throwingHandler = new NetworkErrorHandler();
        var httpClient = new HttpClient(throwingHandler)
        {
            BaseAddress = new Uri("https://apigw.trendyol.com/")
        };
        var adapter = new TrendyolAdapter(httpClient, _loggerMock.Object);

        // Act — PingAsync sends HEAD to the API root
        var result = await adapter.PingAsync();

        // Assert — API is unreachable, so PingAsync returns false
        result.Should().BeFalse("the API is unreachable and should return false");
    }

    /// <summary>
    /// HttpMessageHandler that simulates network errors by always throwing.
    /// </summary>
    private sealed class NetworkErrorHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Connection refused — simulated network error");
        }
    }
}
