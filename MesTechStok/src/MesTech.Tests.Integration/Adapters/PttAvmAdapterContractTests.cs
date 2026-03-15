using System.Net.Http;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// PttAvmAdapter contract tests with WireMock.
/// Verifies Username/Password -> Bearer token flow, order retrieval, stock and price updates.
/// Auth: POST /api/auth/login -> Bearer token
/// Orders: GET /api/orders
/// Stock: PUT /api/product/stock
/// Price: PUT /api/product/price
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "PttAVM")]
public class PttAvmAdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<PttAvmAdapter> _logger;

    private Dictionary<string, string> ValidCredentials => new()
    {
        ["Username"] = "test-pttavm-user",
        ["Password"] = "test-pttavm-pass",
        ["BaseUrl"] = _fixture.BaseUrl,
        ["TokenEndpoint"] = $"{_fixture.BaseUrl}/api/auth/login"
    };

    public PttAvmAdapterContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<PttAvmAdapter>();
    }

    private PttAvmAdapter CreateAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new PttAvmAdapter(httpClient, _logger);
    }

    /// <summary>
    /// Stubs the login endpoint to return a valid Bearer token.
    /// </summary>
    private void StubTokenEndpoint(string token = "pttavm-test-bearer-token", int expiresIn = 3600)
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/auth/login")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""token"":""{token}"",""expiresIn"":{expiresIn}}}"));
    }

    /// <summary>
    /// Creates a fully configured adapter with token obtained.
    /// </summary>
    private async Task<PttAvmAdapter> CreateConfiguredAdapterAsync()
    {
        StubTokenEndpoint();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);
        _fixture.Reset();
        StubTokenEndpoint(); // Re-stub for subsequent calls
        return adapter;
    }

    // ══════════════════════════════════════
    // 1. GetOrdersAsync returns orders on 200
    // ══════════════════════════════════════

    [Fact]
    public async Task PullOrdersAsync_ReturnsOrders_WhenApiResponds200()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/orders")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"": [{
                        ""orderId"": ""PTTAVM-ORD-001"",
                        ""status"": ""Yeni"",
                        ""totalAmount"": 450.00,
                        ""orderDate"": ""2026-03-10T10:00:00.000Z"",
                        ""customerName"": ""Ali Veli"",
                        ""customerPhone"": ""+905551234567"",
                        ""customerAddress"": ""Ankara, Cankaya"",
                        ""customerCity"": ""Ankara"",
                        ""lines"": [{
                            ""sku"": ""SKU-PTT-001"",
                            ""productName"": ""Test Urun"",
                            ""quantity"": 3,
                            ""unitPrice"": 150.00
                        }]
                    }]
                }"));

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().HaveCount(1);
        orders[0].PlatformOrderId.Should().Be("PTTAVM-ORD-001");
        orders[0].PlatformCode.Should().Be("PttAVM");
        orders[0].Status.Should().Be("Yeni");
        orders[0].TotalAmount.Should().Be(450m);
        orders[0].CustomerName.Should().Be("Ali Veli");
        orders[0].Lines.Should().HaveCount(1);
        orders[0].Lines[0].SKU.Should().Be("SKU-PTT-001");
        orders[0].Lines[0].Quantity.Should().Be(3);
        orders[0].Lines[0].UnitPrice.Should().Be(150m);
    }

    // ══════════════════════════════════════
    // 2. GetOrdersAsync returns empty on no data
    // ══════════════════════════════════════

    [Fact]
    public async Task PullOrdersAsync_ReturnsEmpty_WhenNoOrders()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/orders")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""data"": []}"));

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().NotBeNull();
        orders.Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // 3. UpdateStockAsync returns false on 401
    // ══════════════════════════════════════

    [Fact]
    public async Task PushStockUpdateAsync_ReturnsFalse_On401()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/product/stock")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody(@"{""error"": ""Unauthorized""}"));

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 50);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 4. UpdatePriceAsync returns true on success
    // ══════════════════════════════════════

    [Fact]
    public async Task PushPriceUpdateAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/product/price")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""success"": true}"));

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 299.90m);

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════
    // 5. PlatformType is correct
    // ══════════════════════════════════════

    [Fact]
    public void PlatformCode_Returns_PttAVM()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act & Assert
        adapter.PlatformCode.Should().Be("PttAVM");
    }

    // ══════════════════════════════════════
    // 6. SyncProducts_ReturnsCount — PullProductsAsync
    // ══════════════════════════════════════

    [Fact]
    public async Task SyncProducts_ReturnsCount()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/product/list")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"": [
                        {
                            ""productName"": ""PTT Urun A"",
                            ""barcode"": ""SKU-PTT-A"",
                            ""stockQuantity"": 25,
                            ""salePrice"": 199.90
                        },
                        {
                            ""productName"": ""PTT Urun B"",
                            ""barcode"": ""SKU-PTT-B"",
                            ""stockQuantity"": 0,
                            ""salePrice"": 49.50
                        },
                        {
                            ""productName"": ""PTT Urun C"",
                            ""barcode"": ""SKU-PTT-C"",
                            ""stockQuantity"": 100,
                            ""salePrice"": 750.00
                        }
                    ]
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(3);

        products[0].Name.Should().Be("PTT Urun A");
        products[0].SKU.Should().Be("SKU-PTT-A");
        products[0].Stock.Should().Be(25);
        products[0].SalePrice.Should().Be(199.90m);

        products[1].Name.Should().Be("PTT Urun B");
        products[1].SKU.Should().Be("SKU-PTT-B");
        products[1].Stock.Should().Be(0);
        products[1].SalePrice.Should().Be(49.50m);

        products[2].Name.Should().Be("PTT Urun C");
        products[2].SKU.Should().Be("SKU-PTT-C");
        products[2].Stock.Should().Be(100);
        products[2].SalePrice.Should().Be(750m);
    }

    // ══════════════════════════════════════
    // 7. GetCategories_ReturnsList — GetCategoriesAsync
    // ══════════════════════════════════════

    [Fact]
    public async Task GetCategories_ReturnsList()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/category/list")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"": [
                        {
                            ""categoryId"": 100,
                            ""categoryName"": ""Elektronik"",
                            ""subCategories"": [
                                {
                                    ""categoryId"": 110,
                                    ""categoryName"": ""Telefon"",
                                    ""subCategories"": []
                                },
                                {
                                    ""categoryId"": 120,
                                    ""categoryName"": ""Bilgisayar"",
                                    ""subCategories"": []
                                }
                            ]
                        },
                        {
                            ""categoryId"": 200,
                            ""categoryName"": ""Giyim"",
                            ""subCategories"": [
                                {
                                    ""categoryId"": 210,
                                    ""categoryName"": ""Erkek"",
                                    ""subCategories"": []
                                }
                            ]
                        }
                    ]
                }"));

        // Act
        var categories = await adapter.GetCategoriesAsync();

        // Assert
        categories.Should().HaveCount(2, "2 top-level categories: Elektronik, Giyim");

        categories[0].PlatformCategoryId.Should().Be(100);
        categories[0].Name.Should().Be("Elektronik");
        categories[0].ParentId.Should().BeNull("top-level categories have no parent");
        categories[0].SubCategories.Should().HaveCount(2);
        categories[0].SubCategories[0].PlatformCategoryId.Should().Be(110);
        categories[0].SubCategories[0].Name.Should().Be("Telefon");
        categories[0].SubCategories[0].ParentId.Should().Be(100);
        categories[0].SubCategories[1].PlatformCategoryId.Should().Be(120);
        categories[0].SubCategories[1].Name.Should().Be("Bilgisayar");
        categories[0].SubCategories[1].ParentId.Should().Be(100);

        categories[1].PlatformCategoryId.Should().Be(200);
        categories[1].Name.Should().Be("Giyim");
        categories[1].SubCategories.Should().HaveCount(1);
        categories[1].SubCategories[0].PlatformCategoryId.Should().Be(210);
        categories[1].SubCategories[0].Name.Should().Be("Erkek");
        categories[1].SubCategories[0].ParentId.Should().Be(200);
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
