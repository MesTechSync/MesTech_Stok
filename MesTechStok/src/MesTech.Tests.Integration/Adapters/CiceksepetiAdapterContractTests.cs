using System.Net.Http;
using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// CiceksepetiAdapter entegrasyon testleri.
/// WireMock ile gercek CiceksepetiAdapter sinifi test edilir.
/// </summary>
[Trait("Category", "Integration")]
public class CiceksepetiAdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<CiceksepetiAdapter> _logger;

    private static readonly Dictionary<string, string> ValidCredentials = new()
    {
        ["ApiKey"] = "test-cs-api-key"
    };

    public CiceksepetiAdapterContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<CiceksepetiAdapter>();
    }

    private CiceksepetiAdapter CreateAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new CiceksepetiAdapter(httpClient, _logger);
    }

    /// <summary>
    /// Adapter'i yapilandirip kullanima hazir hale getirir.
    /// TestConnectionAsync basarili olursa _isConfigured = true olur.
    /// </summary>
    private async Task<CiceksepetiAdapter> CreateConfiguredAdapterAsync()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""totalCount"":0,""products"":[]}"));

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);
        _fixture.Reset();
        return adapter;
    }

    // ══════════════════════════════════════
    // 1. TestConnectionAsync Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task TestConnectionAsync_ValidApiKey_ReturnsSuccess()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""totalCount"":42,""products"":[]}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Ciceksepeti");
        result.ProductCount.Should().Be(42);
        result.HttpStatusCode.Should().Be(200);
        result.ErrorMessage.Should().BeNull();
        result.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task TestConnectionAsync_InvalidApiKey_ReturnsUnauthorized()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody("Unauthorized"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.HttpStatusCode.Should().Be(401);
        result.ErrorMessage.Should().Contain("Yetkisiz");
    }

    [Fact]
    public async Task TestConnectionAsync_MissingApiKey_ReturnsErrorWithoutHttpCall()
    {
        // Arrange — no WireMock stub needed, should fail before HTTP call
        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(new Dictionary<string, string>());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ApiKey");
        result.HttpStatusCode.Should().BeNull();
        _mockServer.LogEntries.Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // 2. PullProductsAsync Test
    // ══════════════════════════════════════

    [Fact]
    public async Task PullProductsAsync_ReturnsMappedProducts()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""totalCount"": 1,
                    ""products"": [{
                        ""productId"": 101,
                        ""productName"": ""Test Cicek"",
                        ""stockCode"": ""CS-001"",
                        ""barcode"": ""8691234567890"",
                        ""salesPrice"": 99.90,
                        ""stockQuantity"": 15,
                        ""categoryId"": 1,
                        ""images"": []
                    }]
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(1);
        products[0].Name.Should().Be("Test Cicek");
        products[0].SKU.Should().Be("CS-001");
        products[0].Barcode.Should().Be("8691234567890");
        products[0].SalePrice.Should().Be(99.90m);
        products[0].Stock.Should().Be(15);
    }

    // ══════════════════════════════════════
    // 3. PullOrdersAsync — Sub-order Flattening
    // ══════════════════════════════════════

    [Fact]
    public async Task PullOrdersAsync_FlattensSubOrders()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Order")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""totalCount"": 1,
                    ""orders"": [{
                        ""orderId"": 1001,
                        ""orderNumber"": ""CS-ORD-001"",
                        ""orderDate"": ""2026-03-01T10:00:00"",
                        ""customerName"": ""Ali Veli"",
                        ""subOrders"": [
                            {
                                ""subOrderId"": 2001,
                                ""status"": ""New"",
                                ""totalPrice"": 50.00,
                                ""items"": [{""itemId"":1,""stockCode"":""SK1"",""productName"":""Gul"",""quantity"":1,""unitPrice"":50,""totalPrice"":50}]
                            },
                            {
                                ""subOrderId"": 2002,
                                ""status"": ""New"",
                                ""totalPrice"": 30.00,
                                ""items"": [{""itemId"":2,""stockCode"":""SK2"",""productName"":""Papatya"",""quantity"":2,""unitPrice"":15,""totalPrice"":30}]
                            }
                        ]
                    }]
                }"));

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert — 1 order with 2 sub-orders flattened into 2 ExternalOrderDto
        orders.Should().HaveCount(2);
        orders[0].PlatformOrderId.Should().Be("2001");
        orders[1].PlatformOrderId.Should().Be("2002");
        orders[0].CustomerName.Should().Be("Ali Veli");
        orders[0].Lines.Should().HaveCount(1);
        orders[1].Lines.Should().HaveCount(1);
    }

    // ══════════════════════════════════════
    // 4. SendShipmentAsync Test
    // ══════════════════════════════════════

    [Fact]
    public async Task SendShipmentAsync_PostsCorrectPayload()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Order/Shipping")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200));

        // Act
        var result = await adapter.SendShipmentAsync("2001", "YK123456789", CargoProvider.YurticiKargo);

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════
    // 5. PushStockUpdateAsync Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task PushStockUpdateAsync_Success_ReturnsTrue()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products/stock")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200));

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 42);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PushStockUpdateAsync_ServerError_ReturnsFalse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products/stock")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 10);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 6. PushPriceUpdateAsync Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task PushPriceUpdateAsync_Success_ReturnsTrue()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products/price")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200));

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 149.90m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PushPriceUpdateAsync_ServerError_ReturnsFalse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products/price")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 99.90m);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 7. PullProductsAsync — Pagination
    // ══════════════════════════════════════

    [Fact]
    public async Task PullProductsAsync_Paginated_FetchesAllPages()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        // Page 1 — 50 products
        var page1Products = Enumerable.Range(1, 50)
            .Select(i => $@"{{""productId"":{i},""productName"":""Urun {i}"",""stockCode"":""CS-{i:D3}"",""barcode"":""869{i:D10}"",""salesPrice"":{10 + i},""stockQuantity"":{i},""categoryId"":1,""images"":[]}}")
            .ToList();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .WithParam("Page", "1")
                .WithParam("PageSize", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""totalCount"":60,""products"":[{string.Join(",", page1Products)}]}}"));

        // Page 2 — 10 products
        var page2Products = Enumerable.Range(51, 10)
            .Select(i => $@"{{""productId"":{i},""productName"":""Urun {i}"",""stockCode"":""CS-{i:D3}"",""barcode"":""869{i:D10}"",""salesPrice"":{10 + i},""stockQuantity"":{i},""categoryId"":1,""images"":[]}}")
            .ToList();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .WithParam("Page", "2")
                .WithParam("PageSize", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""totalCount"":60,""products"":[{string.Join(",", page2Products)}]}}"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(60);
        products[0].Name.Should().Be("Urun 1");
        products[0].SKU.Should().Be("CS-001");
        products[49].Name.Should().Be("Urun 50");
        products[50].Name.Should().Be("Urun 51");
        products[59].Name.Should().Be("Urun 60");
    }

    [Fact]
    public async Task PullProductsAsync_ServerError_ReturnsPartial()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        // Page 1 — OK with 3 products
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .WithParam("Page", "1")
                .WithParam("PageSize", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""totalCount"": 60,
                    ""products"": [
                        {""productId"":1,""productName"":""Gul"",""stockCode"":""CS-001"",""barcode"":""8691111111111"",""salesPrice"":25.00,""stockQuantity"":10,""categoryId"":1,""images"":[]},
                        {""productId"":2,""productName"":""Lale"",""stockCode"":""CS-002"",""barcode"":""8692222222222"",""salesPrice"":30.00,""stockQuantity"":20,""categoryId"":1,""images"":[]},
                        {""productId"":3,""productName"":""Papatya"",""stockCode"":""CS-003"",""barcode"":""8693333333333"",""salesPrice"":15.00,""stockQuantity"":5,""categoryId"":1,""images"":[]}
                    ]
                }"));

        // Page 2 — 500 error
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .WithParam("Page", "2")
                .WithParam("PageSize", "50")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — only page 1 products returned
        products.Should().HaveCount(3);
        products[0].Name.Should().Be("Gul");
        products[1].Name.Should().Be("Lale");
        products[2].Name.Should().Be("Papatya");
    }

    // ══════════════════════════════════════
    // 8. PullOrdersAsync — Server Error
    // ══════════════════════════════════════

    [Fact]
    public async Task PullOrdersAsync_ServerError_ReturnsEmpty()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Order")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // 9. UpdateOrderStatusAsync Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task UpdateOrderStatusAsync_Success_ReturnsTrue()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Order/Status")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200));

        // Act
        var result = await adapter.UpdateOrderStatusAsync("3001", "Shipped");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ServerError_ReturnsFalse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Order/Status")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act
        var result = await adapter.UpdateOrderStatusAsync("3001", "Shipped");

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 10. GetCategoriesAsync Test
    // ══════════════════════════════════════

    [Fact]
    public async Task GetCategoriesAsync_ReturnsEmptyList()
    {
        // Arrange — GetCategoriesAsync now calls /api/v1/Category
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Categories")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""totalCount"":0,""categories"":[]}"));

        // Act
        var categories = await adapter.GetCategoriesAsync();

        // Assert
        categories.Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // 11. EnsureConfigured Guard Test
    // ══════════════════════════════════════

    [Fact]
    public async Task EnsureConfigured_ThrowsWhenNotConfigured()
    {
        // Arrange — use unconfigured adapter (no TestConnectionAsync call)
        var adapter = CreateAdapter();

        // Act
        var act = () => adapter.PullProductsAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*konfigure*");
    }

    // ══════════════════════════════════════
    // 12. Edge Cases — Empty Results
    // ══════════════════════════════════════

    [Fact]
    public async Task PullProductsAsync_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""totalCount"":0,""products"":[]}"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().BeEmpty();
    }

    [Fact]
    public async Task PullOrdersAsync_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Order")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""totalCount"":0,""orders"":[]}"));

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // 13. Edge Cases — Server Errors
    // ══════════════════════════════════════

    [Fact]
    public async Task SendShipmentAsync_ServerError_ReturnsFalse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Order/Shipping")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act
        var result = await adapter.SendShipmentAsync("2001", "YK123456789", CargoProvider.YurticiKargo);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionAsync_ServerError_ReturnsFalse()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.HttpStatusCode.Should().Be(500);
        result.ErrorMessage.Should().Contain("Ciceksepeti API hatasi");
        result.ErrorMessage.Should().Contain("InternalServerError");
    }

    // ══════════════════════════════════════
    // 14. Edge Cases — Single SubOrder Mapping
    // ══════════════════════════════════════

    [Fact]
    public async Task PullOrdersAsync_SingleSubOrder_MapsCorrectly()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/v1/Order")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""totalCount"": 1,
                    ""orders"": [{
                        ""orderId"": 5001,
                        ""orderNumber"": ""CS-ORD-005"",
                        ""orderDate"": ""2026-03-05T14:30:00"",
                        ""customerName"": ""Ayse Yilmaz"",
                        ""customerEmail"": ""ayse@test.com"",
                        ""deliveryAddress"": ""Ataturk Cad. No:1"",
                        ""deliveryCity"": ""Istanbul"",
                        ""subOrders"": [
                            {
                                ""subOrderId"": 9001,
                                ""status"": ""Preparing"",
                                ""totalPrice"": 75.50,
                                ""cargoCompany"": ""Yurtici Kargo"",
                                ""trackingNumber"": ""YK999888777"",
                                ""items"": [
                                    {""itemId"":10,""stockCode"":""SK-ROSE"",""barcode"":""8690001112223"",""productName"":""Kirmizi Gul Buketi"",""quantity"":2,""unitPrice"":37.75,""totalPrice"":75.50}
                                ]
                            }
                        ]
                    }]
                }"));

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert — single sub-order produces exactly 1 ExternalOrderDto
        orders.Should().HaveCount(1);

        var order = orders[0];
        order.PlatformOrderId.Should().Be("9001");
        order.PlatformCode.Should().Be("Ciceksepeti");
        order.OrderNumber.Should().Be("CS-ORD-005");
        order.Status.Should().Be("Preparing");
        order.CustomerName.Should().Be("Ayse Yilmaz");
        order.CustomerEmail.Should().Be("ayse@test.com");
        order.CustomerAddress.Should().Be("Ataturk Cad. No:1");
        order.CustomerCity.Should().Be("Istanbul");
        order.TotalAmount.Should().Be(75.50m);
        order.CargoProviderName.Should().Be("Yurtici Kargo");
        order.CargoTrackingNumber.Should().Be("YK999888777");
        order.ShipmentPackageId.Should().Be("9001");

        order.Lines.Should().HaveCount(1);
        var line = order.Lines[0];
        line.PlatformLineId.Should().Be("10");
        line.SKU.Should().Be("SK-ROSE");
        line.Barcode.Should().Be("8690001112223");
        line.ProductName.Should().Be("Kirmizi Gul Buketi");
        line.Quantity.Should().Be(2);
        line.UnitPrice.Should().Be(37.75m);
        line.LineTotal.Should().Be(75.50m);
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
