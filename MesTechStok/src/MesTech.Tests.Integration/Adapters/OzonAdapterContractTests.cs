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
/// OzonAdapter contract tests with WireMock.
/// Verifies Client-Id/Api-Key auth, FBS order retrieval, and stock updates.
/// Auth: Client-Id + Api-Key headers (no token exchange)
/// Orders: POST /v3/posting/fbs/list
/// Stock: POST /v2/products/stocks
/// Connection: POST /v1/seller/info
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Ozon")]
public class OzonAdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<OzonAdapter> _logger;

    private Dictionary<string, string> ValidCredentials => new()
    {
        ["ClientId"] = "test-ozon-client-id",
        ["ApiKey"] = "test-ozon-api-key",
        ["BaseUrl"] = _fixture.BaseUrl
    };

    public OzonAdapterContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<OzonAdapter>();
    }

    private OzonAdapter CreateAdapter()
    {
        var httpClient = new HttpClient();
        return new OzonAdapter(httpClient, _logger);
    }

    /// <summary>
    /// Stubs the /v1/seller/info endpoint for TestConnectionAsync.
    /// </summary>
    private void StubSellerInfoEndpoint(string sellerName = "Test Ozon Seller")
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/v1/seller/info")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""result"":{{""name"":""{sellerName}"",""is_enabled"":true}}}}"));
    }

    /// <summary>
    /// Creates a fully configured adapter (TestConnectionAsync passed).
    /// </summary>
    private async Task<OzonAdapter> CreateConfiguredAdapterAsync()
    {
        StubSellerInfoEndpoint();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);
        _fixture.Reset();
        return adapter;
    }

    // ══════════════════════════════════════
    // 1. PlatformCode Test
    // ══════════════════════════════════════

    [Fact]
    public void PlatformCode_Returns_Ozon()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act & Assert
        adapter.PlatformCode.Should().Be("Ozon");
    }

    // ══════════════════════════════════════
    // 2. TestConnectionAsync — Valid Credentials
    // ══════════════════════════════════════

    [Fact]
    public async Task TestConnectionAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        StubSellerInfoEndpoint("My Ozon Store");
        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Ozon");
        result.StoreName.Should().Be("My Ozon Store");
        result.HttpStatusCode.Should().Be(200);
        result.ErrorMessage.Should().BeNull();
        result.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    // ══════════════════════════════════════
    // 3. TestConnectionAsync — Missing Credentials
    // ══════════════════════════════════════

    [Fact]
    public async Task TestConnectionAsync_MissingCredentials_ReturnsError()
    {
        // Arrange — no WireMock stub needed, should fail before HTTP call
        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(new Dictionary<string, string>());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ClientId");
        _mockServer.LogEntries.Should().BeEmpty("no HTTP call should be made with missing credentials");
    }

    // ══════════════════════════════════════
    // 4. PullOrdersAsync — Returns Orders on 200
    // ══════════════════════════════════════

    [Fact]
    public async Task PullOrdersAsync_ReturnsOrders_WhenApiResponds200()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/v3/posting/fbs/list")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""result"": {
                        ""has_next"": false,
                        ""postings"": [{
                            ""posting_number"": ""OZON-POST-001"",
                            ""status"": ""awaiting_deliver"",
                            ""in_process_at"": ""2026-03-10T10:00:00.000Z"",
                            ""analytics_data"": { ""city"": ""Moscow"" },
                            ""products"": [{
                                ""offer_id"": ""SKU-OZ-001"",
                                ""name"": ""Ozon Test Product"",
                                ""quantity"": 3,
                                ""price"": ""150.00""
                            }]
                        }]
                    }
                }"));

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().HaveCount(1);
        orders[0].PlatformOrderId.Should().Be("OZON-POST-001");
        orders[0].PlatformCode.Should().Be("Ozon");
        orders[0].Status.Should().Be("awaiting_deliver");
        orders[0].TotalAmount.Should().Be(450m); // 150 * 3
        orders[0].Lines.Should().HaveCount(1);
        orders[0].Lines[0].SKU.Should().Be("SKU-OZ-001");
        orders[0].Lines[0].Quantity.Should().Be(3);
        orders[0].Lines[0].UnitPrice.Should().Be(150m);
    }

    // ══════════════════════════════════════
    // 5. PullOrdersAsync — Returns Empty on Auth Failure
    // ══════════════════════════════════════

    [Fact]
    public async Task PullOrdersAsync_ReturnsEmpty_WhenApiResponds403()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/v3/posting/fbs/list")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(403)
                .WithBody(@"{""message"":""Invalid credentials"",""code"":16}"));

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().NotBeNull();
        orders.Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // 6. PushStockUpdateAsync — Returns True on Success
    // ══════════════════════════════════════

    [Fact]
    public async Task PushStockUpdateAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/v2/products/stocks")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""result"":[{""warehouse_id"":0,""offer_id"":""" + productId + @""",""updated"":true,""errors"":[]}]}"));

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 100);

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════
    // 7. PushStockUpdateAsync — Returns False on Item Error
    // ══════════════════════════════════════

    [Fact]
    public async Task PushStockUpdateAsync_ReturnsFalse_OnItemError()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/v2/products/stocks")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""result"":[{""warehouse_id"":0,""offer_id"":""" + productId + @""",""updated"":false,""errors"":[{""code"":""NOT_FOUND"",""message"":""Product not found""}]}]}"));

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 50);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 8. PushStockUpdateAsync — Returns False on HTTP Error
    // ══════════════════════════════════════

    [Fact]
    public async Task PushStockUpdateAsync_ReturnsFalse_OnHttpError()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        _mockServer
            .Given(Request.Create()
                .WithPath("/v2/products/stocks")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 10);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 9. EnsureConfigured — Throws When Not Configured
    // ══════════════════════════════════════

    [Fact]
    public async Task EnsureConfigured_ThrowsWhenNotConfigured()
    {
        // Arrange — unconfigured adapter (no TestConnectionAsync call)
        var adapter = CreateAdapter();

        // Act
        Func<Task> act = () => adapter.PullOrdersAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*yapilandirilmadi*");
    }

    // ══════════════════════════════════════
    // 10. SyncProducts_ReturnsCount — PullProductsAsync
    // ══════════════════════════════════════

    [Fact]
    public async Task SyncProducts_ReturnsCount()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        // Stub /v2/product/list — returns 2 product IDs
        _mockServer
            .Given(Request.Create()
                .WithPath("/v2/product/list")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""result"": {
                        ""items"": [
                            { ""product_id"": 100001 },
                            { ""product_id"": 100002 }
                        ],
                        ""last_id"": """"
                    }
                }"));

        // Stub /v2/product/info/list — returns product details
        _mockServer
            .Given(Request.Create()
                .WithPath("/v2/product/info/list")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""result"": {
                        ""items"": [
                            {
                                ""name"": ""Ozon Widget A"",
                                ""offer_id"": ""SKU-OZ-A"",
                                ""barcode"": ""1234567890123"",
                                ""old_price"": ""250.00"",
                                ""stocks"": { ""present"": 10 },
                                ""visible"": true
                            },
                            {
                                ""name"": ""Ozon Widget B"",
                                ""offer_id"": ""SKU-OZ-B"",
                                ""barcode"": ""9876543210987"",
                                ""old_price"": ""99.50"",
                                ""stocks"": { ""present"": 0 },
                                ""visible"": false
                            }
                        ]
                    }
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(2);

        products[0].Name.Should().Be("Ozon Widget A");
        products[0].SKU.Should().Be("SKU-OZ-A");
        products[0].Barcode.Should().Be("1234567890123");
        products[0].SalePrice.Should().Be(250m);
        products[0].Stock.Should().Be(10);
        products[0].IsActive.Should().BeTrue();

        products[1].Name.Should().Be("Ozon Widget B");
        products[1].SKU.Should().Be("SKU-OZ-B");
        products[1].SalePrice.Should().Be(99.5m);
        products[1].Stock.Should().Be(0);
        products[1].IsActive.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 11. GetCategories_ReturnsCategoryTree
    // ══════════════════════════════════════

    [Fact]
    public async Task GetCategories_ReturnsCategoryTree()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        // Stub /v1/description-category/tree — returns nested category tree
        _mockServer
            .Given(Request.Create()
                .WithPath("/v1/description-category/tree")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""result"": [
                        {
                            ""description_category_id"": 1,
                            ""category_name"": ""Elektronik"",
                            ""children"": [
                                {
                                    ""description_category_id"": 10,
                                    ""category_name"": ""Telefonlar"",
                                    ""children"": []
                                },
                                {
                                    ""description_category_id"": 11,
                                    ""category_name"": ""Tabletler"",
                                    ""children"": []
                                }
                            ]
                        },
                        {
                            ""description_category_id"": 2,
                            ""category_name"": ""Giyim"",
                            ""children"": []
                        }
                    ]
                }"));

        // Act
        var categories = await adapter.GetCategoriesAsync();

        // Assert
        categories.Should().HaveCount(2);

        // First top-level category — Elektronik
        categories[0].PlatformCategoryId.Should().Be(1);
        categories[0].Name.Should().Be("Elektronik");
        categories[0].ParentId.Should().BeNull();
        categories[0].SubCategories.Should().HaveCount(2);

        // Nested child — Telefonlar
        categories[0].SubCategories[0].PlatformCategoryId.Should().Be(10);
        categories[0].SubCategories[0].Name.Should().Be("Telefonlar");
        categories[0].SubCategories[0].ParentId.Should().Be(1);
        categories[0].SubCategories[0].SubCategories.Should().BeEmpty();

        // Nested child — Tabletler
        categories[0].SubCategories[1].PlatformCategoryId.Should().Be(11);
        categories[0].SubCategories[1].Name.Should().Be("Tabletler");
        categories[0].SubCategories[1].ParentId.Should().Be(1);

        // Second top-level category — Giyim (no children)
        categories[1].PlatformCategoryId.Should().Be(2);
        categories[1].Name.Should().Be("Giyim");
        categories[1].ParentId.Should().BeNull();
        categories[1].SubCategories.Should().BeEmpty();
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
