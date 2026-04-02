using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Adapters;

[Trait("Category", "Integration")]
[Trait("Layer", "Adapter")]
public class ShopifyAdapterTests
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly Mock<ILogger<ShopifyAdapter>> _loggerMock = new();

    private ShopifyAdapter CreateAdapter()
    {
        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://test-store.myshopify.com/")
        };
        return new ShopifyAdapter(httpClient, _loggerMock.Object);
    }

    private static Dictionary<string, string> ValidCredentials() => new()
    {
        ["ShopDomain"] = "test-store.myshopify.com",
        ["AccessToken"] = "shpat_test_token_12345",
        ["LocationId"] = "98765",
        ["WebhookSecret"] = "whsec_test_secret_abc"
    };

    /// <summary>
    /// Configures adapter via TestConnectionAsync with valid mock responses.
    /// Enqueues: shop.json (200) + products/count.json (200).
    /// </summary>
    private async Task ConfigureAdapterAsync(ShopifyAdapter adapter)
    {
        // shop.json response
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"shop":{"id":1,"name":"Test Store","email":"test@example.com"}}""");
        // products/count.json response
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"count":42}""");
        await adapter.TestConnectionAsync(ValidCredentials());
    }

    // ─────────────────────────────────────────────
    // 1. TestConnection_ValidShop_ReturnsSuccess
    // ─────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_ValidShop_ReturnsSuccess()
    {
        // Arrange
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"shop":{"id":1,"name":"My Shopify Store","email":"owner@shop.com"}}""");
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"count":150}""");

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Shopify");
        result.ProductCount.Should().Be(150);
        result.StoreName.Should().Be("My Shopify Store");
        result.HttpStatusCode.Should().Be(200);

        // Verify correct endpoints were called
        _handler.CapturedRequests.Should().HaveCount(2);
        _handler.CapturedRequests[0].RequestUri!.ToString()
            .Should().Contain("/admin/api/2024-01/shop.json");
        _handler.CapturedRequests[1].RequestUri!.ToString()
            .Should().Contain("/admin/api/2024-01/products/count.json");
    }

    // ─────────────────────────────────────────────
    // 2. TestConnection_InvalidCredentials_ReturnsFailure
    // ─────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_InvalidCredentials_ReturnsFailure()
    {
        // Arrange — Shopify returns 401 Unauthorized
        _handler.EnqueueResponse(HttpStatusCode.Unauthorized,
            """{"errors":"[API] Invalid API key or access token (unrecognized login or wrong password)"}""");

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.PlatformCode.Should().Be("Shopify");
        result.HttpStatusCode.Should().Be(401);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().Contain("Unauthorized");
    }

    // ─────────────────────────────────────────────
    // 3. PullProducts_ReturnsProducts
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PullProducts_ReturnsProducts()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        _handler.EnqueueResponse(HttpStatusCode.OK,
            """
            {
              "products": [
                {
                  "id": 1001,
                  "title": "Blue T-Shirt",
                  "vendor": "TestVendor",
                  "variants": [
                    {"id": 2001, "sku": "BTS-001", "price": "29.99", "inventory_quantity": 50, "inventory_item_id": 3001}
                  ]
                },
                {
                  "id": 1002,
                  "title": "Red Cap",
                  "vendor": "TestVendor",
                  "variants": [
                    {"id": 2002, "sku": "RC-002", "price": "14.50", "inventory_quantity": 100, "inventory_item_id": 3002}
                  ]
                }
              ]
            }
            """);

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(2);
        products[0].Name.Should().Be("Blue T-Shirt");
        products[0].SKU.Should().Be("BTS-001");
        products[0].SalePrice.Should().Be(29.99m);
        products[0].Stock.Should().Be(50);
        products[1].Name.Should().Be("Red Cap");
        products[1].SKU.Should().Be("RC-002");
        products[1].SalePrice.Should().Be(14.50m);
        products[1].Stock.Should().Be(100);
    }

    // ─────────────────────────────────────────────
    // 4. PullProducts_EmptyStore_ReturnsEmpty
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PullProducts_EmptyStore_ReturnsEmpty()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"products":[]}""");

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    // 5. PullProducts_Pagination_MultiplePages
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PullProducts_Pagination_MultiplePages()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        // Page 1 — has Link header with rel="next"
        var page1Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "products": [
                    {
                      "id": 1,
                      "title": "Product A",
                      "variants": [{"id": 101, "sku": "SKU-A", "price": "10.00", "inventory_quantity": 5, "inventory_item_id": 201}]
                    }
                  ]
                }
                """, Encoding.UTF8, "application/json")
        };
        page1Response.Headers.Add("Link",
            "<https://test-store.myshopify.com/admin/api/2024-01/products.json?page_info=cursor2&limit=250>; rel=\"next\"");
        _handler.EnqueueResponse(page1Response);

        // Page 2 — no Link header (final page)
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """
            {
              "products": [
                {
                  "id": 2,
                  "title": "Product B",
                  "variants": [{"id": 102, "sku": "SKU-B", "price": "20.00", "inventory_quantity": 8, "inventory_item_id": 202}]
                }
              ]
            }
            """);

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(2);
        products[0].Name.Should().Be("Product A");
        products[0].SKU.Should().Be("SKU-A");
        products[1].Name.Should().Be("Product B");
        products[1].SKU.Should().Be("SKU-B");

        // Verify two product requests were made (page 1 + page 2)
        var productRequests = _handler.CapturedRequests
            .Where(r => r.RequestUri!.ToString().Contains("products.json"))
            .ToList();
        productRequests.Should().HaveCount(2);
    }

    // ─────────────────────────────────────────────
    // 6. PushStockUpdate_Success_ReturnsTrue
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PushStockUpdate_Success_ReturnsTrue()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        var productId = Guid.NewGuid();
        var sku = productId.ToString();

        // Step 1: GET variants.json — returns matching variant with inventory_item_id
        _handler.EnqueueResponse(HttpStatusCode.OK,
            $$"""
            {
              "variants": [
                {"id": 5001, "sku": "{{sku}}", "inventory_item_id": 7001}
              ]
            }
            """);

        // Step 2: POST inventory_levels/set.json — success
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"inventory_level":{"inventory_item_id":7001,"location_id":98765,"available":25}}""");

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 25);

        // Assert
        result.Should().BeTrue();

        // Verify inventory_levels/set.json was called
        var setRequest = _handler.CapturedRequests.Last();
        setRequest.RequestUri!.ToString().Should().Contain("/inventory_levels/set.json");
        setRequest.Method.Should().Be(HttpMethod.Post);
    }

    // ─────────────────────────────────────────────
    // 7. PushStockUpdate_ProductNotFound_ReturnsFalse
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PushStockUpdate_ProductNotFound_ReturnsFalse()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        // GET variants.json — returns variants but none match the SKU
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"variants":[{"id":5001,"sku":"DIFFERENT-SKU","inventory_item_id":7001}]}""");

        var productId = Guid.NewGuid();

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 10);

        // Assert
        result.Should().BeFalse("variant with matching SKU was not found");
    }

    // ─────────────────────────────────────────────
    // 8. PushPriceUpdate_Success_ReturnsTrue
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PushPriceUpdate_Success_ReturnsTrue()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        var productId = Guid.NewGuid();
        var sku = productId.ToString();

        // Step 1: GET variants.json — returns matching variant with id
        _handler.EnqueueResponse(HttpStatusCode.OK,
            $$"""
            {
              "variants": [
                {"id": 6001, "sku": "{{sku}}"}
              ]
            }
            """);

        // Step 2: PUT variants/6001.json — success
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"variant":{"id":6001,"price":"49.99"}}""");

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 49.99m);

        // Assert
        result.Should().BeTrue();

        // Verify PUT was called on the correct variant endpoint
        var putRequest = _handler.CapturedRequests.Last();
        putRequest.RequestUri!.ToString().Should().Contain("/variants/6001.json");
        putRequest.Method.Should().Be(HttpMethod.Put);
    }

    // ─────────────────────────────────────────────
    // 9. PushPriceUpdate_NotFound_ReturnsFalse
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PushPriceUpdate_NotFound_ReturnsFalse()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        // GET variants.json — returns variants but none match the SKU
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"variants":[{"id":6001,"sku":"NONEXISTENT-SKU"}]}""");

        var productId = Guid.NewGuid();

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 99.90m);

        // Assert
        result.Should().BeFalse("variant with matching SKU was not found");
    }

    // ─────────────────────────────────────────────
    // 10. PullOrders_ReturnsOrders
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PullOrders_ReturnsOrders()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        _handler.EnqueueResponse(HttpStatusCode.OK,
            """
            {
              "orders": [
                {
                  "id": 9001,
                  "name": "#1001",
                  "financial_status": "paid",
                  "created_at": "2026-03-10T10:00:00Z",
                  "updated_at": "2026-03-10T12:00:00Z",
                  "total_price": "79.98",
                  "currency": "USD",
                  "customer": {
                    "first_name": "Jane",
                    "last_name": "Doe",
                    "email": "jane@example.com",
                    "phone": "+1234567890"
                  },
                  "shipping_address": {
                    "address1": "123 Main St",
                    "city": "New York"
                  },
                  "line_items": [
                    {
                      "id": 11001,
                      "sku": "BTS-001",
                      "title": "Blue T-Shirt",
                      "quantity": 2,
                      "price": "29.99",
                      "total_discount": "0.00",
                      "tax_lines": [{"rate": 0.08, "price": "4.80", "title": "State Tax"}]
                    }
                  ]
                }
              ]
            }
            """);

        // Act
        var orders = await adapter.PullOrdersAsync(DateTime.UtcNow.AddDays(-7));

        // Assert
        orders.Should().HaveCount(1);
        var order = orders[0];
        order.PlatformCode.Should().Be("Shopify");
        order.PlatformOrderId.Should().Be("9001");
        order.OrderNumber.Should().Be("#1001");
        order.Status.Should().Be("paid");
        order.TotalAmount.Should().Be(79.98m);
        order.Currency.Should().Be("USD");
        order.CustomerName.Should().Be("Jane Doe");
        order.CustomerEmail.Should().Be("jane@example.com");
        order.CustomerPhone.Should().Be("+1234567890");
        order.CustomerAddress.Should().Be("123 Main St");
        order.CustomerCity.Should().Be("New York");

        order.Lines.Should().HaveCount(1);
        var line = order.Lines[0];
        line.SKU.Should().Be("BTS-001");
        line.ProductName.Should().Be("Blue T-Shirt");
        line.Quantity.Should().Be(2);
        line.UnitPrice.Should().Be(29.99m);
        line.TaxRate.Should().Be(8m); // 0.08 * 100
    }

    // ─────────────────────────────────────────────
    // 11. PullOrders_EmptyResult_ReturnsEmptyList
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PullOrders_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"orders":[]}""");

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    // 12. PlatformCode_IsShopify
    // ─────────────────────────────────────────────

    [Fact]
    public void PlatformCode_IsShopify()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Assert
        adapter.PlatformCode.Should().Be("Shopify");
        adapter.SupportsStockUpdate.Should().BeTrue();
        adapter.SupportsPriceUpdate.Should().BeTrue();
        adapter.SupportsShipment.Should().BeTrue();
    }

    // ─────────────────────────────────────────────
    // 13. VerifyWebhookSignature_ValidHmac_ReturnsTrue
    // ─────────────────────────────────────────────

    [Fact]
    public async Task VerifyWebhookSignature_ValidHmac_ReturnsTrue()
    {
        // Arrange — configure adapter with WebhookSecret via TestConnectionAsync
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        var secret = "whsec_test_secret_abc";
        var payload = """{"id":9001,"financial_status":"paid"}""";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        // Compute the expected HMAC-SHA256 signature
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        var validSignature = Convert.ToBase64String(hash);

        // Act
        var result = adapter.VerifyWebhookSignature(payloadBytes, validSignature);

        // Assert
        result.Should().BeTrue("the signature was computed with the correct secret");
    }

    // ─────────────────────────────────────────────
    // 14. VerifyWebhookSignature_InvalidHmac_ReturnsFalse
    // ─────────────────────────────────────────────

    [Fact]
    public async Task VerifyWebhookSignature_InvalidHmac_ReturnsFalse()
    {
        // Arrange — configure adapter with WebhookSecret
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        var payload = """{"id":9001,"financial_status":"paid"}""";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        // Use a completely wrong signature
        var invalidSignature = Convert.ToBase64String(Encoding.UTF8.GetBytes("totally-wrong-hmac"));

        // Act
        var result = adapter.VerifyWebhookSignature(payloadBytes, invalidSignature);

        // Assert
        result.Should().BeFalse("the signature does not match the expected HMAC");
    }

    // ─────────────────────────────────────────────
    // 15. SupportsPriceUpdate_ReturnsExpectedValue
    // ─────────────────────────────────────────────

    [Fact]
    public void SupportsPriceUpdate_ReturnsExpectedValue()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Assert
        adapter.SupportsPriceUpdate.Should().BeTrue(
            "Shopify adapter supports variant price updates via PUT /variants/{id}.json");
    }

    // ─────────────────────────────────────────────
    // 16. TestConnection_MissingShopDomain_ReturnsFailure
    // ─────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_MissingShopDomain_ReturnsFailure()
    {
        // Arrange — credentials without ShopDomain
        var creds = new Dictionary<string, string>
        {
            ["AccessToken"] = "shpat_test_token"
        };

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(creds);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.PlatformCode.Should().Be("Shopify");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ─────────────────────────────────────────────
    // 17. PushStockUpdate_NoLocationId_ReturnsFalse
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PushStockUpdate_NoLocationId_ReturnsFalse()
    {
        // Arrange — configure without LocationId
        var credsNoLocation = new Dictionary<string, string>
        {
            ["ShopDomain"] = "test-store.myshopify.com",
            ["AccessToken"] = "shpat_test_token_12345"
            // No LocationId — stock update requires it
        };

        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"shop":{"id":1,"name":"Test Store"}}""");
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"count":5}""");

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(credsNoLocation);

        // Act
        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 10);

        // Assert
        result.Should().BeFalse("LocationId is required for inventory level updates");
    }

    // ─────────────────────────────────────────────
    // 18. PullOrders_WithCustomerAndShippingAddress_MapsCorrectly
    // ─────────────────────────────────────────────

    [Fact]
    public async Task PullOrders_WithCustomerAndShippingAddress_MapsCorrectly()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        _handler.EnqueueResponse(HttpStatusCode.OK,
            """
            {
              "orders": [
                {
                  "id": 9002,
                  "name": "#1002",
                  "financial_status": "pending",
                  "created_at": "2026-03-15T08:30:00Z",
                  "total_price": "149.50",
                  "total_discounts": "10.00",
                  "currency": "TRY",
                  "total_shipping_price_set": {
                    "shop_money": {"amount": "15.00", "currency_code": "TRY"}
                  },
                  "customer": {
                    "first_name": "Ahmet",
                    "last_name": "Yilmaz",
                    "email": "ahmet@example.com"
                  },
                  "shipping_address": {
                    "address1": "Ataturk Cad.",
                    "address2": "No: 42",
                    "city": "Istanbul",
                    "phone": "+905551234567"
                  },
                  "line_items": [
                    {
                      "id": 11002,
                      "sku": "JACKET-L",
                      "title": "Winter Jacket",
                      "quantity": 1,
                      "price": "149.50",
                      "total_discount": "10.00",
                      "tax_lines": [{"rate": 0.20, "price": "29.90", "title": "KDV"}]
                    }
                  ]
                }
              ]
            }
            """);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().HaveCount(1);
        var order = orders[0];
        order.CustomerName.Should().Be("Ahmet Yilmaz");
        order.CustomerAddress.Should().Be("Ataturk Cad. No: 42");
        order.CustomerCity.Should().Be("Istanbul");
        order.CustomerPhone.Should().Be("+905551234567");
        order.ShippingCost.Should().Be(15.00m);
        order.DiscountAmount.Should().Be(10.00m);
        order.Currency.Should().Be("TRY");

        var line = order.Lines[0];
        line.DiscountAmount.Should().Be(10.00m);
        line.TaxRate.Should().Be(20m); // 0.20 * 100
    }
}
