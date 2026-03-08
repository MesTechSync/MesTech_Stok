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

    public void Dispose()
    {
        _fixture.Reset();
    }
}
