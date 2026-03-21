using System.Net;
using System.Net.Http.Headers;
using System.Text;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Adapters;

[Trait("Category", "Integration")]
[Trait("Layer", "Adapter")]
public class WooCommerceAdapterTests
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly Mock<ILogger<WooCommerceAdapter>> _loggerMock = new();

    private WooCommerceAdapter CreateAdapter()
    {
        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://mystore.example.com/")
        };
        return new WooCommerceAdapter(httpClient, _loggerMock.Object);
    }

    private static Dictionary<string, string> ValidCredentials() => new()
    {
        ["SiteUrl"] = "https://mystore.example.com",
        ["ConsumerKey"] = "ck_test_key_123",
        ["ConsumerSecret"] = "cs_test_secret_456"
    };

    /// <summary>
    /// Configures the adapter by running TestConnectionAsync with valid credentials.
    /// Enqueues both the system_status and the product-count responses.
    /// </summary>
    private async Task ConfigureAdapterAsync(WooCommerceAdapter adapter)
    {
        // system_status response
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"environment":{"site_url":"https://mystore.example.com"}}""");
        // product count response (X-WP-Total header set via custom response)
        var countResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        };
        countResponse.Headers.Add("X-WP-Total", "10");
        _handler.EnqueueResponse(countResponse);

        await adapter.TestConnectionAsync(ValidCredentials());
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. PlatformCode_IsWooCommerce
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public void PlatformCode_IsWooCommerce()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Assert
        adapter.PlatformCode.Should().Be("WooCommerce");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. TestConnection_ValidStore_ReturnsSuccess
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TestConnection_ValidStore_ReturnsSuccess()
    {
        // Arrange
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"environment":{"site_url":"https://mystore.example.com"}}""");

        var countResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        };
        countResponse.Headers.Add("X-WP-Total", "42");
        _handler.EnqueueResponse(countResponse);

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("WooCommerce");
        result.StoreName.Should().Be("https://mystore.example.com");
        result.ProductCount.Should().Be(42);
        result.HttpStatusCode.Should().Be(200);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. TestConnection_InvalidCredentials_ReturnsFailure
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TestConnection_InvalidCredentials_ReturnsFailure()
    {
        // Arrange
        _handler.EnqueueResponse(HttpStatusCode.Unauthorized,
            """{"code":"woocommerce_rest_cannot_view","message":"Unauthorized"}""");

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.HttpStatusCode.Should().Be(401);
        result.ErrorMessage.Should().NotBeNull();
        result.ErrorMessage.Should().Contain("Unauthorized");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. PullProducts_ReturnsProducts
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task PullProducts_ReturnsProducts()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        var productsJson = """
            [
              {"id":101,"name":"Widget A","sku":"WID-001","price":"29.99","stock_quantity":50,"status":"publish"},
              {"id":102,"name":"Widget B","sku":"WID-002","price":"49.50","stock_quantity":20,"status":"publish"}
            ]
            """;
        var productsResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(productsJson, Encoding.UTF8, "application/json")
        };
        productsResponse.Headers.Add("X-WP-TotalPages", "1");
        _handler.EnqueueResponse(productsResponse);

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Count.Should().Be(2);
        products[0].Name.Should().Be("Widget A");
        products[0].SKU.Should().Be("WID-001");
        products[0].SalePrice.Should().Be(29.99m);
        products[0].Stock.Should().Be(50);
        products[1].Name.Should().Be("Widget B");
        products[1].SKU.Should().Be("WID-002");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. PullProducts_EmptyStore_ReturnsEmpty
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task PullProducts_EmptyStore_ReturnsEmpty()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        var emptyResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        };
        emptyResponse.Headers.Add("X-WP-TotalPages", "1");
        _handler.EnqueueResponse(emptyResponse);

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().BeEmpty();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. PullProducts_Pagination_MultiplePages_AllReturned
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task PullProducts_Pagination_MultiplePages_AllReturned()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        // Page 1 — 2 products, X-WP-TotalPages=2
        var page1Json = """
            [
              {"id":1,"name":"Product 1","sku":"P-001","price":"10.00","stock_quantity":5,"status":"publish"},
              {"id":2,"name":"Product 2","sku":"P-002","price":"20.00","stock_quantity":15,"status":"publish"}
            ]
            """;
        var page1Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(page1Json, Encoding.UTF8, "application/json")
        };
        page1Response.Headers.Add("X-WP-TotalPages", "2");
        _handler.EnqueueResponse(page1Response);

        // Page 2 — 1 product, X-WP-TotalPages=2
        var page2Json = """
            [
              {"id":3,"name":"Product 3","sku":"P-003","price":"30.00","stock_quantity":8,"status":"publish"}
            ]
            """;
        var page2Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(page2Json, Encoding.UTF8, "application/json")
        };
        page2Response.Headers.Add("X-WP-TotalPages", "2");
        _handler.EnqueueResponse(page2Response);

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Count.Should().Be(3);
        products[0].Name.Should().Be("Product 1");
        products[1].Name.Should().Be("Product 2");
        products[2].Name.Should().Be("Product 3");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. PushStockUpdate_Success_ReturnsTrue
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task PushStockUpdate_Success_ReturnsTrue()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        var productId = Guid.NewGuid();

        // Search by SKU — returns 1 match
        _handler.EnqueueResponse(HttpStatusCode.OK,
            "[{\"id\":42,\"name\":\"Found Product\",\"sku\":\"" + productId + "\"}]");

        // PUT stock update — success
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"id":42,"stock_quantity":99,"manage_stock":true}""");

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 99);

        // Assert
        result.Should().BeTrue();

        // Verify the PUT request was made (search + put = requests after config)
        var putRequest = _handler.CapturedRequests.Last();
        putRequest.Method.Should().Be(HttpMethod.Put);
        putRequest.RequestUri!.ToString().Should().Contain("/products/42");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 8. PushStockUpdate_ProductNotFound_ReturnsFalse
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task PushStockUpdate_ProductNotFound_ReturnsFalse()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        var productId = Guid.NewGuid();

        // Search by SKU — empty result
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 10);

        // Assert
        result.Should().BeFalse();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 9. PushPriceUpdateAsync_FindsProduct_UpdatesRegularPrice
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task PushPriceUpdateAsync_FindsProduct_UpdatesRegularPrice()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        var productId = Guid.NewGuid();

        // Search by SKU — returns 1 match
        _handler.EnqueueResponse(HttpStatusCode.OK,
            "[{\"id\":77,\"name\":\"Price Test Product\",\"sku\":\"" + productId + "\"}]");

        // PUT price update — success
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"id":77,"regular_price":"149.99"}""");

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 149.99m);

        // Assert
        result.Should().BeTrue();

        // Verify the PUT request body contains regular_price
        var putRequest = _handler.CapturedRequests.Last();
        putRequest.Method.Should().Be(HttpMethod.Put);
        putRequest.RequestUri!.ToString().Should().Contain("/products/77");

        var body = await putRequest.Content!.ReadAsStringAsync();
        body.Should().Contain("regular_price");
        body.Should().Contain("149.99");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 10. PushPriceUpdateAsync_ProductNotFound_ReturnsFalse
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task PushPriceUpdateAsync_ProductNotFound_ReturnsFalse()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        var productId = Guid.NewGuid();

        // Search by SKU — empty result
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 50.00m);

        // Assert
        result.Should().BeFalse();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 11. GetOrders_ReturnsOrders
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetOrders_ReturnsOrders()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        var ordersJson = """
            [
              {
                "id": 501,
                "number": "501",
                "status": "processing",
                "date_created": "2026-03-15T10:30:00",
                "currency": "USD",
                "total": "120.50",
                "discount_total": "10.00",
                "billing": {
                  "first_name": "John",
                  "last_name": "Doe",
                  "email": "john@example.com",
                  "phone": "+1-555-1234",
                  "address_1": "123 Main St",
                  "address_2": "",
                  "city": "New York"
                },
                "line_items": [
                  {
                    "id": 1001,
                    "name": "Widget A",
                    "sku": "WID-001",
                    "quantity": 2,
                    "price": "55.25",
                    "total": "110.50"
                  }
                ]
              }
            ]
            """;
        var ordersResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ordersJson, Encoding.UTF8, "application/json")
        };
        ordersResponse.Headers.Add("X-WP-TotalPages", "1");
        _handler.EnqueueResponse(ordersResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().ContainSingle();
        var order = orders[0];
        order.PlatformOrderId.Should().Be("501");
        order.PlatformCode.Should().Be("WooCommerce");
        order.Status.Should().Be("processing");
        order.TotalAmount.Should().Be(120.50m);
        order.DiscountAmount.Should().Be(10.00m);
        order.Currency.Should().Be("USD");
        order.CustomerName.Should().Be("John Doe");
        order.CustomerEmail.Should().Be("john@example.com");
        order.CustomerPhone.Should().Be("+1-555-1234");
        order.CustomerAddress.Should().Be("123 Main St");
        order.CustomerCity.Should().Be("New York");
        order.Lines.Should().ContainSingle();
        order.Lines[0].SKU.Should().Be("WID-001");
        order.Lines[0].Quantity.Should().Be(2);
        order.Lines[0].UnitPrice.Should().Be(55.25m);
        order.Lines[0].LineTotal.Should().Be(110.50m);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 12. GetOrders_EmptyResult_ReturnsEmptyList
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetOrders_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        var emptyResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        };
        emptyResponse.Headers.Add("X-WP-TotalPages", "1");
        _handler.EnqueueResponse(emptyResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().BeEmpty();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 13. GetOrders_MultipleOrders_AllMapped
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetOrders_MultipleOrders_AllMapped()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        var ordersJson = """
            [
              {
                "id": 601,
                "number": "601",
                "status": "processing",
                "date_created": "2026-03-14T08:00:00",
                "currency": "EUR",
                "total": "200.00",
                "discount_total": "0.00",
                "billing": {"first_name":"Alice","last_name":"Smith","email":"alice@example.com"},
                "line_items": [
                  {"id":2001,"name":"Item X","sku":"IX-01","quantity":1,"price":"200.00","total":"200.00"}
                ]
              },
              {
                "id": 602,
                "number": "602",
                "status": "processing",
                "date_created": "2026-03-14T09:00:00",
                "currency": "TRY",
                "total": "750.00",
                "discount_total": "50.00",
                "billing": {"first_name":"Bob","last_name":"Jones","email":"bob@example.com"},
                "line_items": [
                  {"id":2002,"name":"Item Y","sku":"IY-01","quantity":3,"price":"250.00","total":"750.00"}
                ]
              },
              {
                "id": 603,
                "number": "603",
                "status": "processing",
                "date_created": "2026-03-14T10:00:00",
                "currency": "USD",
                "total": "15.00",
                "discount_total": "0.00",
                "billing": {"first_name":"Charlie","last_name":"Brown","email":"charlie@example.com"},
                "line_items": [
                  {"id":2003,"name":"Item Z","sku":"IZ-01","quantity":1,"price":"15.00","total":"15.00"}
                ]
              }
            ]
            """;
        var ordersResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ordersJson, Encoding.UTF8, "application/json")
        };
        ordersResponse.Headers.Add("X-WP-TotalPages", "1");
        _handler.EnqueueResponse(ordersResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Count.Should().Be(3);

        orders[0].PlatformOrderId.Should().Be("601");
        orders[0].Currency.Should().Be("EUR");
        orders[0].CustomerName.Should().Be("Alice Smith");

        orders[1].PlatformOrderId.Should().Be("602");
        orders[1].Currency.Should().Be("TRY");
        orders[1].TotalAmount.Should().Be(750.00m);
        orders[1].DiscountAmount.Should().Be(50.00m);

        orders[2].PlatformOrderId.Should().Be("603");
        orders[2].Currency.Should().Be("USD");
        orders[2].CustomerName.Should().Be("Charlie Brown");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 14. SupportsPriceUpdate_ReturnsTrue
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public void SupportsPriceUpdate_ReturnsTrue()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Assert
        adapter.SupportsPriceUpdate.Should().BeTrue();
        adapter.SupportsStockUpdate.Should().BeTrue();
        adapter.SupportsShipment.Should().BeFalse();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 15. TestConnection_NetworkError_ReturnsFailure
    // ────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TestConnection_NetworkError_ReturnsFailure()
    {
        // Arrange — use a handler that throws HttpRequestException to simulate network error
        var throwingHandler = new NetworkErrorHttpMessageHandler();
        var httpClient = new HttpClient(throwingHandler)
        {
            BaseAddress = new Uri("https://mystore.example.com/")
        };
        var adapter = new WooCommerceAdapter(httpClient, _loggerMock.Object);

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNull();
        result.PlatformCode.Should().Be("WooCommerce");
    }

    /// <summary>
    /// Custom HttpMessageHandler that always throws HttpRequestException
    /// to simulate network timeout/connectivity errors.
    /// </summary>
    private sealed class NetworkErrorHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("A connection attempt failed — simulated network error");
        }
    }
}
