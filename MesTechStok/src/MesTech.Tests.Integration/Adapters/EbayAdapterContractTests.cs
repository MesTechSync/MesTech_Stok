using System.Net.Http;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// EbayAdapter contract tests with WireMock.
/// Verifies OAuth2 token flow, Fulfillment API (orders), and Inventory API (stock).
/// Auth: OAuth2 Client Credentials → Bearer token
/// Orders: GET /sell/fulfillment/v1/order
/// Stock: PUT /sell/inventory/v1/inventory_item/{sku}
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "eBay")]
public class EbayAdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<EbayAdapter> _logger;

    private Dictionary<string, string> ValidCredentials => new()
    {
        ["ClientId"] = "test-ebay-client-id",
        ["ClientSecret"] = "test-ebay-client-secret",
        ["TokenEndpoint"] = $"{_fixture.BaseUrl}/identity/v1/oauth2/token",
        ["BaseUrl"] = _fixture.BaseUrl
    };

    public EbayAdapterContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<EbayAdapter>();
    }

    private EbayAdapter CreateAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new EbayAdapter(httpClient, _logger);
    }

    /// <summary>
    /// Stubs the OAuth2 token endpoint to return a valid access token.
    /// </summary>
    private void StubTokenEndpoint(string accessToken = "v^1.1|test-access-token", int expiresIn = 7200)
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/identity/v1/oauth2/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{""access_token"":""{accessToken}"",""expires_in"":{expiresIn},""token_type"":""Application Access Token""}}"));
    }

    /// <summary>
    /// Creates a fully configured adapter with token obtained.
    /// </summary>
    private async Task<EbayAdapter> CreateConfiguredAdapterAsync()
    {
        StubTokenEndpoint();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);
        _fixture.Reset();
        StubTokenEndpoint(); // Re-stub for subsequent calls
        return adapter;
    }

    // ══════════════════════════════════════
    // 1. PlatformCode Test
    // ══════════════════════════════════════

    [Fact]
    public void PlatformCode_Returns_eBay()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act & Assert
        adapter.PlatformCode.Should().Be("eBay");
    }

    // ══════════════════════════════════════
    // 2. TestConnectionAsync — Valid Credentials
    // ══════════════════════════════════════

    [Fact]
    public async Task TestConnectionAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        StubTokenEndpoint();
        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("eBay");
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
                .WithPath("/sell/fulfillment/v1/order")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""total"": 1,
                    ""orders"": [{
                        ""orderId"": ""EBAY-ORD-001"",
                        ""orderFulfillmentStatus"": ""NOT_STARTED"",
                        ""creationDate"": ""2026-03-10T10:00:00.000Z"",
                        ""pricingSummary"": {
                            ""total"": { ""value"": ""250.00"", ""currency"": ""USD"" }
                        },
                        ""buyer"": { ""username"": ""ebay_buyer_42"" },
                        ""lineItems"": [{
                            ""lineItemId"": ""LI-001"",
                            ""sku"": ""SKU-EBAY-001"",
                            ""title"": ""Test Product"",
                            ""quantity"": 2,
                            ""lineItemCost"": { ""value"": ""250.00"", ""currency"": ""USD"" }
                        }]
                    }]
                }"));

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().HaveCount(1);
        orders[0].PlatformOrderId.Should().Be("EBAY-ORD-001");
        orders[0].PlatformCode.Should().Be("eBay");
        orders[0].Status.Should().Be("NOT_STARTED");
        orders[0].TotalAmount.Should().Be(250m);
        orders[0].CustomerName.Should().Be("ebay_buyer_42");
        orders[0].Lines.Should().HaveCount(1);
        orders[0].Lines[0].SKU.Should().Be("SKU-EBAY-001");
        orders[0].Lines[0].Quantity.Should().Be(2);
    }

    // ══════════════════════════════════════
    // 5. PullOrdersAsync — Returns Empty on 401
    // ══════════════════════════════════════

    [Fact]
    public async Task PullOrdersAsync_ReturnsEmpty_WhenApiResponds401()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/sell/fulfillment/v1/order")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody(@"{""errors"":[{""errorId"":1001,""message"":""Invalid access token""}]}"));

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
        var encodedSku = Uri.EscapeDataString(productId.ToString());

        // Stub GET (fetch existing inventory item) — might return 404 if new
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sell/inventory/v1/inventory_item/{encodedSku}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""sku"":""" + productId + @""",""product"":{""title"":""Existing Product""},""availability"":{""shipToLocationAvailability"":{""quantity"":5}}}"));

        // Stub PUT (update stock)
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sell/inventory/v1/inventory_item/{encodedSku}")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(204));

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 50);

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════
    // 7. PushStockUpdateAsync — Returns False on Failure
    // ══════════════════════════════════════

    [Fact]
    public async Task PushStockUpdateAsync_ReturnsFalse_OnFailure()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();
        var encodedSku = Uri.EscapeDataString(productId.ToString());

        // Stub GET
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sell/inventory/v1/inventory_item/{encodedSku}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404));

        // Stub PUT to return 400 (invalid request)
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sell/inventory/v1/inventory_item/{encodedSku}")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithBody(@"{""errors"":[{""errorId"":25002,""message"":""Invalid inventory item""}]}"));

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 10);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 8. EnsureConfigured — Throws When Not Configured
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
    // 9. SyncProducts — Returns Count When Inventory Items Exist
    // ══════════════════════════════════════

    [Fact]
    public async Task SyncProducts_ReturnsCount_WhenInventoryItemsExist()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/sell/inventory/v1/inventory_item")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""total"": 3,
                    ""inventoryItems"": [
                        {
                            ""sku"": ""EBAY-SKU-001"",
                            ""product"": { ""title"": ""Wireless Mouse"" },
                            ""availability"": { ""shipToLocationAvailability"": { ""quantity"": 150 } }
                        },
                        {
                            ""sku"": ""EBAY-SKU-002"",
                            ""product"": { ""title"": ""Mechanical Keyboard"" },
                            ""availability"": { ""shipToLocationAvailability"": { ""quantity"": 75 } }
                        },
                        {
                            ""sku"": ""EBAY-SKU-003"",
                            ""product"": { ""title"": ""USB-C Hub"" },
                            ""availability"": { ""shipToLocationAvailability"": { ""quantity"": 0 } }
                        }
                    ]
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(3);
        products[0].SKU.Should().Be("EBAY-SKU-001");
        products[0].Name.Should().Be("Wireless Mouse");
        products[0].Stock.Should().Be(150);
        products[1].SKU.Should().Be("EBAY-SKU-002");
        products[1].Name.Should().Be("Mechanical Keyboard");
        products[1].Stock.Should().Be(75);
        products[2].SKU.Should().Be("EBAY-SKU-003");
        products[2].Name.Should().Be("USB-C Hub");
        products[2].Stock.Should().Be(0);
    }

    // ══════════════════════════════════════
    // 10. GetCategories — Returns Category Tree
    // ══════════════════════════════════════

    [Fact]
    public async Task GetCategories_ReturnsCategoryTree()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/commerce/taxonomy/v1/category_tree/3")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""categoryTreeId"": ""3"",
                    ""categoryTreeVersion"": ""119"",
                    ""rootCategoryNode"": {
                        ""category"": { ""categoryId"": ""0"", ""categoryName"": ""Root"" },
                        ""childCategoryTreeNodes"": [
                            {
                                ""category"": { ""categoryId"": ""550"", ""categoryName"": ""Art"" },
                                ""childCategoryTreeNodes"": [
                                    {
                                        ""category"": { ""categoryId"": ""551"", ""categoryName"": ""Digital Art"" }
                                    },
                                    {
                                        ""category"": { ""categoryId"": ""552"", ""categoryName"": ""Paintings"" }
                                    }
                                ]
                            },
                            {
                                ""category"": { ""categoryId"": ""625"", ""categoryName"": ""Electronics"" },
                                ""childCategoryTreeNodes"": [
                                    {
                                        ""category"": { ""categoryId"": ""626"", ""categoryName"": ""Computers"" }
                                    }
                                ]
                            }
                        ]
                    }
                }"));

        // Act
        var categories = await adapter.GetCategoriesAsync();

        // Assert
        categories.Should().HaveCount(2, "root has 2 top-level children: Art, Electronics");
        categories[0].PlatformCategoryId.Should().Be(550);
        categories[0].Name.Should().Be("Art");
        categories[0].ParentId.Should().BeNull("top-level categories have no parent");
        categories[0].SubCategories.Should().HaveCount(2);
        categories[0].SubCategories[0].PlatformCategoryId.Should().Be(551);
        categories[0].SubCategories[0].Name.Should().Be("Digital Art");
        categories[0].SubCategories[0].ParentId.Should().Be(550);
        categories[0].SubCategories[1].PlatformCategoryId.Should().Be(552);
        categories[0].SubCategories[1].Name.Should().Be("Paintings");
        categories[1].PlatformCategoryId.Should().Be(625);
        categories[1].Name.Should().Be("Electronics");
        categories[1].SubCategories.Should().HaveCount(1);
        categories[1].SubCategories[0].PlatformCategoryId.Should().Be(626);
        categories[1].SubCategories[0].Name.Should().Be("Computers");
        categories[1].SubCategories[0].ParentId.Should().Be(625);
    }

    // ══════════════════════════════════════
    // 11. SyncProducts — When API Down Returns Zero
    // ══════════════════════════════════════

    [Fact]
    public async Task SyncProducts_WhenApiDown_ReturnsZero()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/sell/inventory/v1/inventory_item")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithBody(@"{""errors"":[{""errorId"":1000,""message"":""Service temporarily unavailable""}]}"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().NotBeNull();
        products.Should().BeEmpty("API returned 503 Service Unavailable");
    }

    // ══════════════════════════════════════
    // 12. PushPriceUpdateAsync — Returns True When Offer Found
    // ══════════════════════════════════════

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Platform", "eBay")]
    public async Task PushPriceUpdateAsync_ReturnsTrue_WhenOfferFound()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();
        var encodedSku = Uri.EscapeDataString(productId.ToString());
        const string offerId = "OFFER-ABC-001";

        // Stub GET /sell/inventory/v1/offer?sku={sku} → returns offer with offerId
        _mockServer
            .Given(Request.Create()
                .WithPath("/sell/inventory/v1/offer")
                .WithParam("sku", encodedSku)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($@"{{
                    ""total"": 1,
                    ""offers"": [{{
                        ""offerId"": ""{offerId}"",
                        ""sku"": ""{productId}"",
                        ""pricingSummary"": {{
                            ""price"": {{ ""value"": ""99.99"", ""currency"": ""USD"" }}
                        }}
                    }}]
                }}"));

        // Stub PUT /sell/inventory/v1/offer/{offerId} → 204 No Content
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sell/inventory/v1/offer/{offerId}")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(204));

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 149.99m);

        // Assert
        result.Should().BeTrue("PUT offer returned 204 No Content");
    }

    // ══════════════════════════════════════
    // 13. PushPriceUpdateAsync — Returns False When No Offer Found
    // ══════════════════════════════════════

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Platform", "eBay")]
    public async Task PushPriceUpdateAsync_ReturnsFalse_WhenNoOfferFound()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();
        var encodedSku = Uri.EscapeDataString(productId.ToString());

        // Stub GET /sell/inventory/v1/offer?sku={sku} → returns empty offers list
        _mockServer
            .Given(Request.Create()
                .WithPath("/sell/inventory/v1/offer")
                .WithParam("sku", encodedSku)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""total"": 0, ""offers"": []}"));

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 99.99m);

        // Assert
        result.Should().BeFalse("no offers exist for the given SKU");
    }

    // ══════════════════════════════════════
    // 14. GetCategoriesAsync — Returns Empty When API Responds 404
    // ══════════════════════════════════════

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Platform", "eBay")]
    public async Task GetCategoriesAsync_ReturnsEmpty_WhenApiResponds404()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/commerce/taxonomy/v1/category_tree/3")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody(@"{""errors"":[{""errorId"":20006,""message"":""Category tree not found""}]}"));

        // Act
        var categories = await adapter.GetCategoriesAsync();

        // Assert
        categories.Should().NotBeNull();
        categories.Should().BeEmpty("API returned 404 Not Found");
    }

    // ══════════════════════════════════════
    // 15. PushProductAsync — Returns False (Not Yet Implemented)
    // ══════════════════════════════════════

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Platform", "eBay")]
    public async Task PushProductAsync_ReturnsFalse_NotYetImplemented()
    {
        // Arrange — PushProductAsync logs a warning and returns false (3-step flow not yet implemented)
        StubTokenEndpoint();
        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        var product = new MesTech.Domain.Entities.Product
        {
            Name = "Test Product",
            SKU = "SKU-PUSH-001",
            Stock = 10,
            SalePrice = 29.99m
        };

        // Act
        var result = await adapter.PushProductAsync(product);

        // Assert
        result.Should().BeFalse("full eBay listing creation (3-step inventory+offer+publish) is not yet implemented");
    }

    // ══════════════════════════════════════
    // 16. Token Refresh After Expiry — Token Endpoint Called Twice
    // ══════════════════════════════════════

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Platform", "eBay")]
    public async Task GetAccessToken_RefreshesToken_WhenExpiredOnSecondCall()
    {
        // Arrange — first token expires almost immediately (1 second)
        _fixture.Reset();

        // First token: expires_in=1 second so it will be stale on the second call (5-min buffer kicks in)
        _mockServer
            .Given(Request.Create()
                .WithPath("/identity/v1/oauth2/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""access_token"":""first-token"",""expires_in"":1,""token_type"":""Application Access Token""}"));

        var adapter = CreateAdapter();

        // First call: configures adapter and obtains first token
        var firstResult = await adapter.TestConnectionAsync(ValidCredentials);
        firstResult.IsSuccess.Should().BeTrue();

        // The first token expires_in=1s, but with the 5-minute buffer (300s), the adapter
        // will always try to refresh because 1 - 300 < 0. This means every call triggers a refresh.
        // Verify that a second independent TestConnectionAsync (which calls GetAccessTokenAsync)
        // will also hit the token endpoint.
        _fixture.Reset();
        _mockServer
            .Given(Request.Create()
                .WithPath("/identity/v1/oauth2/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""access_token"":""second-token"",""expires_in"":7200,""token_type"":""Application Access Token""}"));

        // Stub a downstream API call that also triggers GetAccessTokenAsync
        _mockServer
            .Given(Request.Create()
                .WithPath("/sell/fulfillment/v1/order")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""total"":0,""orders"":[]}"));

        // Act — second call; token is stale so adapter must refresh
        var secondOrders = await adapter.PullOrdersAsync();

        // Assert
        secondOrders.Should().NotBeNull();
        secondOrders.Should().BeEmpty();
        // Token endpoint was called again (once for this second invocation)
        _mockServer.LogEntries
            .Count(e => e.RequestMessage.Path == "/identity/v1/oauth2/token")
            .Should().Be(1, "token endpoint hit once for the second (stale-token) call");
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
