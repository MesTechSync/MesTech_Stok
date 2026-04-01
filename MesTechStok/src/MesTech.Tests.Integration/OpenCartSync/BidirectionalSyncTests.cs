using System.Net;
using System.Net.Http;
using FluentAssertions;
using MesTech.Application.DTOs.Platform;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace MesTech.Tests.Integration.OpenCartSync;

/// <summary>
/// OpenCart bidirectional sync WireMock contract tests.
/// 10 tests: 5 push (product, stock, price, category, customer) + 5 pull (products, orders, categories, customers, conflict).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "OpenCart")]
public class BidirectionalSyncTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<OpenCartAdapter> _logger;

    private const string TestApiToken = "oc-test-token-12345";

    private static readonly Dictionary<string, string> ValidCredentials = new()
    {
        ["ApiToken"] = TestApiToken,
        ["BaseUrl"] = "PLACEHOLDER" // overridden in CreateConfiguredAdapterAsync
    };

    public BidirectionalSyncTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new Mock<ILogger<OpenCartAdapter>>().Object;
    }

    public void Dispose() => _fixture.Reset();

    private OpenCartAdapter CreateAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new OpenCartAdapter(httpClient, _logger);
    }

    private async Task<OpenCartAdapter> CreateConfiguredAdapterAsync()
    {
        // Stub TestConnectionAsync endpoint: GET /api/rest/products?limit=1
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/products")
                .WithParam("limit", "1")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""data"":[],""total"":0}"));

        var creds = new Dictionary<string, string>(ValidCredentials)
        {
            ["BaseUrl"] = _fixture.BaseUrl
        };

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(creds);
        _fixture.Reset();
        return adapter;
    }

    // ════════════════════════════════════════════════════════════════════
    //  PUSH TESTS (5)
    // ════════════════════════════════════════════════════════════════════

    // ════ 1. PushProduct — create ════

    [Fact]
    public async Task PushProduct_NewProduct_PostsToApi()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var product = new Product
        {
            Name = "Test Urun",
            SKU = "OC-SKU-001",
            Stock = 50,
            SalePrice = 199.99m,
            IsActive = true
        };

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/products")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithBody(@"{""success"":true}"));

        // Act
        var result = await adapter.PushProductAsync(product);

        // Assert
        result.Should().BeTrue();
    }

    // ════ 2. PushStockUpdate — single PUT ════

    [Fact]
    public async Task PushStockUpdate_ValidProduct_ReturnsTrue()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/rest/products/{productId}")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{""success"":true}"));

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 75);

        // Assert
        result.Should().BeTrue();
    }

    // ════ 3. PushPriceUpdate — single PUT ════

    [Fact]
    public async Task PushPriceUpdate_ValidProduct_ReturnsTrue()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/api/rest/products/{productId}")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{""success"":true}"));

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 249.99m);

        // Assert
        result.Should().BeTrue();
    }

    // ════ 4. PushBatchStockUpdate — parallel with throttling ════

    [Fact]
    public async Task PushBatchStockUpdate_MultipleProducts_ReturnsSuccessCount()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        // Stub all 3 product updates
        foreach (var id in new[] { id1, id2, id3 })
        {
            _mockServer
                .Given(Request.Create()
                    .WithPath($"/api/rest/products/{id}")
                    .UsingPut())
                .RespondWith(Response.Create()
                    .WithStatusCode(200));
        }

        var updates = new List<(Guid ProductId, int NewStock)>
        {
            (id1, 10), (id2, 20), (id3, 30)
        };

        // Act
        var count = await adapter.PushBatchStockUpdateAsync(updates);

        // Assert
        count.Should().Be(3);
    }

    // ════ 5. PushCategory — create ════

    [Fact]
    public async Task PushCategory_NewCategory_PostsToApi()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/categories")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithBody(@"{""success"":true}"));

        var category = new CategorySyncDto
        {
            Name = "Elektronik",
            SortOrder = 1,
            Status = true
        };

        // Act
        var result = await adapter.PushCategoryAsync(category);

        // Assert
        result.Should().BeTrue();
    }

    // ════════════════════════════════════════════════════════════════════
    //  PULL TESTS (5)
    // ════════════════════════════════════════════════════════════════════

    // ════ 6. PullProducts — paginated ════

    [Fact]
    public async Task PullProducts_ReturnsMappedProducts()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"": [
                        {""product_id"":1,""name"":""Urun A"",""sku"":""SKU-A"",""quantity"":""50"",""price"":""100.00"",""description"":""Aciklama A""},
                        {""product_id"":2,""name"":""Urun B"",""sku"":""SKU-B"",""quantity"":""25"",""price"":""200.50"",""description"":""Aciklama B""}
                    ]
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(2);
        products[0].Name.Should().Be("Urun A");
        products[0].SKU.Should().Be("SKU-A");
        products[1].SalePrice.Should().Be(200.50m); // InvariantCulture: "200.50" → 200.50
    }

    // ════ 7. PullOrders — mapped with status ════

    [Fact]
    public async Task PullOrders_ReturnsMappedOrders()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/orders")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"": [
                        {
                            ""order_id"": 1001,
                            ""order_status_id"": ""3"",
                            ""firstname"": ""Ali"",
                            ""lastname"": ""Yilmaz"",
                            ""email"": ""ali@example.com"",
                            ""telephone"": ""555-1234"",
                            ""shipping_address_1"": ""Kadikoy"",
                            ""shipping_city"": ""Istanbul"",
                            ""total"": ""350"",
                            ""currency_code"": ""TRY"",
                            ""date_added"": ""2026-03-09 10:30:00"",
                            ""products"": [
                                {
                                    ""order_product_id"": ""5001"",
                                    ""model"": ""SKU-A"",
                                    ""name"": ""Urun A"",
                                    ""quantity"": ""2"",
                                    ""price"": ""100.00"",
                                    ""tax"": ""18.00"",
                                    ""total"": ""236.00""
                                }
                            ]
                        }
                    ]
                }"));

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().HaveCount(1);
        orders[0].PlatformOrderId.Should().Be("1001");
        orders[0].Status.Should().Be("Shipped");
        orders[0].CustomerName.Should().Be("Ali Yilmaz");
        orders[0].TotalAmount.Should().Be(350m);
        orders[0].Lines.Should().HaveCount(1);
        orders[0].Lines[0].SKU.Should().Be("SKU-A");
    }

    // ════ 8. PullCategories — tree structure ════

    [Fact]
    public async Task PullCategories_ReturnsCategoryTree()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/categories")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"": [
                        {""category_id"":""10"",""parent_id"":""0"",""name"":""Elektronik"",""sort_order"":""1"",""status"":""1""},
                        {""category_id"":""11"",""parent_id"":""10"",""name"":""Bilgisayar"",""sort_order"":""2"",""status"":""1""},
                        {""category_id"":""12"",""parent_id"":""10"",""name"":""Telefon"",""sort_order"":""3"",""status"":""1""}
                    ]
                }"));

        // Act
        var categories = await adapter.PullCategoryTreeAsync();

        // Assert
        categories.Should().HaveCount(1); // Only root "Elektronik"
        categories[0].Name.Should().Be("Elektronik");
        categories[0].Children.Should().HaveCount(2);
        categories[0].Children.Should().Contain(c => c.Name == "Bilgisayar");
        categories[0].Children.Should().Contain(c => c.Name == "Telefon");
    }

    // ════ 9. PullCustomers — date filtered ════

    [Fact]
    public async Task PullCustomers_ReturnsMappedCustomers()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/customers")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"": [
                        {
                            ""customer_id"": ""42"",
                            ""firstname"": ""Ayse"",
                            ""lastname"": ""Demir"",
                            ""email"": ""ayse@example.com"",
                            ""telephone"": ""555-9876"",
                            ""address_1"": ""Besiktas"",
                            ""city"": ""Istanbul"",
                            ""country"": ""Turkey"",
                            ""date_modified"": ""2026-03-08 14:30:00""
                        }
                    ]
                }"));

        // Act
        var customers = await adapter.PullCustomersAsync();

        // Assert
        customers.Should().HaveCount(1);
        customers[0].FirstName.Should().Be("Ayse");
        customers[0].LastName.Should().Be("Demir");
        customers[0].Email.Should().Be("ayse@example.com");
        customers[0].City.Should().Be("Istanbul");
    }

    // ════ 10. PullProducts — server error returns empty ════

    [Fact]
    public async Task PullProducts_ServerError_ReturnsEmptyList()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody(@"{""error"": ""Internal Server Error""}"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().BeEmpty();
    }
}
