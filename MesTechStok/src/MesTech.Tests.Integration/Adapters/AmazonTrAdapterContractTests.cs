using System.Net.Http;
using System.Xml.Linq;
using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// AmazonTrAdapter entegrasyon testleri.
/// WireMock ile gercek AmazonTrAdapter sinifi test edilir.
/// SP-API + LWA OAuth2 + Feeds (XDocument) dogrulanir.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Amazon")]
public class AmazonTrAdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<AmazonTrAdapter> _logger;

    private const string SellerId = "ATEST123SELLER";
    private const string MarketplaceId = "A33AVAJ2PDY3EV";

    private Dictionary<string, string> ValidCredentials => new()
    {
        ["RefreshToken"] = "Atzr|test-refresh-token",
        ["ClientId"] = "amzn1.application-oa2-client.test",
        ["ClientSecret"] = "test-client-secret",
        ["SellerId"] = SellerId,
        ["BaseUrl"] = _fixture.BaseUrl,
        ["LwaEndpoint"] = $"{_fixture.BaseUrl}/auth/o2/token"
    };

    public AmazonTrAdapterContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<AmazonTrAdapter>();
    }

    private AmazonTrAdapter CreateAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new AmazonTrAdapter(httpClient, _logger);
    }

    /// <summary>
    /// Stubs the LWA token endpoint to return a valid access token.
    /// </summary>
    private void StubLwaTokenEndpoint(string accessToken = "Atza|test-access-token", int expiresIn = 3600)
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/auth/o2/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(AmazonWireMockHelper.BuildLwaTokenResponse(accessToken, expiresIn)));
    }

    /// <summary>
    /// Stubs the catalog items endpoint for TestConnectionAsync.
    /// </summary>
    private void StubCatalogEndpoint(int numberOfResults = 42)
    {
        var items = numberOfResults > 0
            ? new[] { ("B00TEST001", "Test Product", "TST-001") }
            : Array.Empty<(string, string, string)>();

        _mockServer
            .Given(Request.Create()
                .WithPath("/catalog/2022-04-01/items")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(AmazonWireMockHelper.BuildCatalogItemsResponse(items)));
    }

    /// <summary>
    /// Creates a fully configured adapter (LWA token + catalog health check passed).
    /// </summary>
    private async Task<AmazonTrAdapter> CreateConfiguredAdapterAsync()
    {
        StubLwaTokenEndpoint();
        StubCatalogEndpoint();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);
        _fixture.Reset();
        StubLwaTokenEndpoint(); // Re-stub for subsequent calls
        return adapter;
    }

    // ══════════════════════════════════════
    // 1. Auth Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task TestConnectionAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        StubLwaTokenEndpoint();
        StubCatalogEndpoint();

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StoreName.Should().Contain(SellerId);
        result.HttpStatusCode.Should().Be(200);
        result.ErrorMessage.Should().BeNull();
        result.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task TestConnectionAsync_MissingRefreshToken_ReturnsError()
    {
        // Arrange
        var adapter = CreateAdapter();
        var credentials = new Dictionary<string, string>
        {
            ["ClientId"] = "test",
            ["ClientSecret"] = "test",
            ["SellerId"] = SellerId,
            ["BaseUrl"] = _fixture.BaseUrl,
            ["LwaEndpoint"] = $"{_fixture.BaseUrl}/auth/o2/token"
        };

        // Act
        var result = await adapter.TestConnectionAsync(credentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("zorunlu");
        _mockServer.LogEntries.Should().BeEmpty("no HTTP call should be made");
    }

    [Fact]
    public async Task TestConnectionAsync_LwaReturns401_ReturnsError()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/auth/o2/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody(AmazonWireMockHelper.BuildLwaErrorResponse("invalid_client", "Invalid client credentials")));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Configure_SetsMarketplaceCorrectly()
    {
        // Arrange
        StubLwaTokenEndpoint();
        StubCatalogEndpoint();

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert — verify that catalog request used correct marketplace ID
        result.IsSuccess.Should().BeTrue();
        var catalogRequests = _mockServer.LogEntries
            .Where(e => e.RequestMessage.Path?.Contains("/catalog/") == true);
        catalogRequests.Should().NotBeEmpty();
        catalogRequests.First().RequestMessage.RawQuery.Should().Contain(MarketplaceId);
    }

    [Fact]
    public async Task EnsureFreshToken_CachedToken_SkipsRefresh()
    {
        // Arrange — configure adapter (gets token), then make 2 catalog calls
        StubLwaTokenEndpoint();
        StubCatalogEndpoint();

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);

        // Count token requests so far (should be 1)
        var initialTokenRequests = _mockServer.LogEntries
            .Count(e => e.RequestMessage.Path?.Contains("/auth/o2/token") == true);
        initialTokenRequests.Should().Be(1);

        // Act — make another call (PullProducts, which should reuse the cached token)
        _fixture.Reset();
        StubLwaTokenEndpoint(); // Re-stub but it should NOT be called
        StubCatalogEndpoint();

        var products = await adapter.PullProductsAsync();

        // Assert — verify no additional token request was made (token cached)
        var newTokenRequests = _mockServer.LogEntries
            .Count(e => e.RequestMessage.Path?.Contains("/auth/o2/token") == true);
        newTokenRequests.Should().Be(0, "token should be cached from TestConnectionAsync");
    }

    // ══════════════════════════════════════
    // 2. Catalog Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task PullProductsAsync_Success_MapsCorrectly()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        var items = new[]
        {
            ("B00ASIN001", "Mavi Tisort", "SKU-001"),
            ("B00ASIN002", "Kirmizi Gomlek", "SKU-002")
        };

        _mockServer
            .Given(Request.Create()
                .WithPath("/catalog/2022-04-01/items")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(AmazonWireMockHelper.BuildCatalogItemsResponse(items)));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().HaveCount(2);
        products[0].Name.Should().Be("Mavi Tisort");
        products[0].SKU.Should().Be("SKU-001");
        products[0].Barcode.Should().Be("B00ASIN001");
        products[1].Name.Should().Be("Kirmizi Gomlek");
        products[1].SKU.Should().Be("SKU-002");
    }

    [Fact]
    public async Task PullProductsAsync_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/catalog/2022-04-01/items")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(AmazonWireMockHelper.BuildCatalogItemsResponse()));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().BeEmpty();
    }

    [Fact]
    public async Task PushProductAsync_Success_ReturnsTrue()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/listings/2021-08-01/items/{SellerId}/*")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(AmazonWireMockHelper.BuildPutListingsItemResponse("TST-001", "ACCEPTED")));

        var product = new Product
        {
            Name = "Test Urun",
            SKU = "TST-001",
            SalePrice = 99.90m,
            CategoryId = Guid.NewGuid()
        };

        // Act
        var result = await adapter.PushProductAsync(product);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsEmptyList()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        // Act
        var categories = await adapter.GetCategoriesAsync();

        // Assert
        categories.Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // 3. Orders Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task PullOrdersAsync_Success_MapsOrders()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        var orders = new[]
        {
            ("ORD-001", "Shipped", "2026-03-10T10:00:00Z"),
            ("ORD-002", "Pending", "2026-03-10T11:00:00Z")
        };

        _mockServer
            .Given(Request.Create()
                .WithPath("/orders/v0/orders")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(AmazonWireMockHelper.BuildOrdersResponse(orders)));

        // Stub order items for each order
        foreach (var (orderId, _, _) in orders)
        {
            _mockServer
                .Given(Request.Create()
                    .WithPath($"/orders/v0/orders/{orderId}/orderItems")
                    .UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(AmazonWireMockHelper.BuildOrderItemsResponse(orderId,
                        new[] { ("SKU-A", 2, 75.00m) })));
        }

        // Act
        var result = await adapter.PullOrdersAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].PlatformOrderId.Should().Be("ORD-001");
        result[0].Status.Should().Be("Shipped");
        result[0].PlatformCode.Should().Be("Amazon");
        result[0].Lines.Should().HaveCount(1);
        result[0].Lines[0].SKU.Should().Be("SKU-A");
        result[0].Lines[0].Quantity.Should().Be(2);
    }

    [Fact]
    public async Task PullOrdersAsync_WithDateFilter_PassesCreatedAfter()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var since = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        _mockServer
            .Given(Request.Create()
                .WithPath("/orders/v0/orders")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(AmazonWireMockHelper.BuildOrdersResponse()));

        // Act
        var result = await adapter.PullOrdersAsync(since);

        // Assert
        result.Should().BeEmpty();
        var orderRequests = _mockServer.LogEntries
            .Where(e => e.RequestMessage.Path?.Contains("/orders/v0/orders") == true
                        && !e.RequestMessage.Path.Contains("orderItems"));
        orderRequests.Should().NotBeEmpty();
        orderRequests.First().RequestMessage.RawQuery.Should().Contain("CreatedAfter=");
    }

    [Fact]
    public async Task PullOrdersAsync_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/orders/v0/orders")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(AmazonWireMockHelper.BuildOrdersResponse()));

        // Act
        var result = await adapter.PullOrdersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_NotSupported_ReturnsFalse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        // Act
        var result = await adapter.UpdateOrderStatusAsync("PKG-001", "Shipped");

        // Assert — Amazon does not support direct order status update
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 4. Feeds Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task PushStockUpdateAsync_Success_SubmitsFeed()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        StubFeedSubmissionEndpoints();

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 100);

        // Assert
        result.Should().BeTrue();

        // Verify feed document was created
        var docRequests = _mockServer.LogEntries
            .Where(e => e.RequestMessage.Path?.Contains("/feeds/2021-06-30/documents") == true);
        docRequests.Should().NotBeEmpty();

        // Verify feed was created
        var feedRequests = _mockServer.LogEntries
            .Where(e => e.RequestMessage.Path == "/feeds/2021-06-30/feeds"
                        && e.RequestMessage.Method == "POST");
        feedRequests.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PushStockUpdateAsync_FeedCreateFails_ReturnsFalse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        // Step 1: feed document creation fails
        _mockServer
            .Given(Request.Create()
                .WithPath("/feeds/2021-06-30/documents")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithBody(AmazonWireMockHelper.BuildErrorResponse("InvalidInput", "Bad content type")));

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 50);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PushPriceUpdateAsync_Success_SubmitsFeed()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var productId = Guid.NewGuid();

        StubFeedSubmissionEndpoints();

        // Act
        var result = await adapter.PushPriceUpdateAsync(productId, 199.99m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PushPriceUpdateAsync_XDocumentOutput_IsValidXml()
    {
        // Arrange — directly test the XML builder (internal method)
        var adapter = await CreateConfiguredAdapterAsync();

        // Act
        var xml = adapter.BuildPricingFeed("TST-001", 149.90m);

        // Assert — validate XDocument structure
        xml.Should().NotBeNull();
        xml.Root.Should().NotBeNull();
        xml.Root!.Name.LocalName.Should().Be("AmazonEnvelope");

        var header = xml.Root.Element("Header");
        header.Should().NotBeNull();
        header!.Element("DocumentVersion")!.Value.Should().Be("1.01");

        var messageType = xml.Root.Element("MessageType")!.Value;
        messageType.Should().Be("Price");

        var message = xml.Root.Element("Message");
        message.Should().NotBeNull();
        var price = message!.Element("Price");
        price.Should().NotBeNull();
        price!.Element("SKU")!.Value.Should().Be("TST-001");
        price.Element("StandardPrice")!.Value.Should().Be("149.90");
        price.Element("StandardPrice")!.Attribute("currency")!.Value.Should().Be("TRY");
    }

    [Fact]
    public async Task PushStockUpdateAsync_XDocumentOutput_IsValidXml()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        // Act
        var xml = adapter.BuildInventoryFeed("TST-002", 75);

        // Assert
        xml.Should().NotBeNull();
        xml.Root!.Name.LocalName.Should().Be("AmazonEnvelope");

        var messageType = xml.Root.Element("MessageType")!.Value;
        messageType.Should().Be("Inventory");

        var inventory = xml.Root.Element("Message")!.Element("Inventory");
        inventory.Should().NotBeNull();
        inventory!.Element("SKU")!.Value.Should().Be("TST-002");
        inventory.Element("Quantity")!.Value.Should().Be("75");
    }

    // ══════════════════════════════════════
    // 5. Edge Case Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task AllMethods_Unconfigured_ThrowInvalidOperation()
    {
        // Arrange — adapter created without calling TestConnectionAsync
        var adapter = CreateAdapter();
        var product = new Product { Name = "Test", SKU = "SKU-001", SalePrice = 10m, CategoryId = Guid.NewGuid() };

        // Act & Assert — all data methods should throw
        var pullProducts = () => adapter.PullProductsAsync();
        await pullProducts.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*yapilandirilmadi*");

        var pushProduct = () => adapter.PushProductAsync(product);
        await pushProduct.Should().ThrowAsync<InvalidOperationException>();

        var pushStock = () => adapter.PushStockUpdateAsync(Guid.NewGuid(), 10);
        await pushStock.Should().ThrowAsync<InvalidOperationException>();

        var pushPrice = () => adapter.PushPriceUpdateAsync(Guid.NewGuid(), 99.99m);
        await pushPrice.Should().ThrowAsync<InvalidOperationException>();

        var pullOrders = () => adapter.PullOrdersAsync();
        await pullOrders.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void PlatformCode_ReturnsAmazon()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act & Assert
        adapter.PlatformCode.Should().Be("Amazon");
    }

    [Fact]
    public void Capabilities_AllTrue()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act & Assert
        adapter.SupportsStockUpdate.Should().BeTrue();
        adapter.SupportsPriceUpdate.Should().BeTrue();
        adapter.SupportsShipment.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_MissingClientId_ReturnsError()
    {
        // Arrange
        var adapter = CreateAdapter();
        var credentials = new Dictionary<string, string>
        {
            ["RefreshToken"] = "test",
            ["ClientSecret"] = "test",
            ["SellerId"] = SellerId,
            ["BaseUrl"] = _fixture.BaseUrl,
            ["LwaEndpoint"] = $"{_fixture.BaseUrl}/auth/o2/token"
        };

        // Act
        var result = await adapter.TestConnectionAsync(credentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("zorunlu");
    }

    [Fact]
    public async Task TestConnectionAsync_EmptyRefreshToken_ReturnsError()
    {
        // Arrange
        var adapter = CreateAdapter();
        var credentials = new Dictionary<string, string>
        {
            ["RefreshToken"] = "  ",
            ["ClientId"] = "test",
            ["ClientSecret"] = "test",
            ["SellerId"] = SellerId,
            ["BaseUrl"] = _fixture.BaseUrl,
            ["LwaEndpoint"] = $"{_fixture.BaseUrl}/auth/o2/token"
        };

        // Act
        var result = await adapter.TestConnectionAsync(credentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("zorunlu");
    }

    // ══════════════════════════════════════
    // Helper: Stub full feed submission flow
    // ══════════════════════════════════════

    private void StubFeedSubmissionEndpoints()
    {
        // Step 1: Create feed document
        _mockServer
            .Given(Request.Create()
                .WithPath("/feeds/2021-06-30/documents")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(AmazonWireMockHelper.BuildCreateFeedDocumentResponse(
                    "amzn1.tortuga.3.test-doc",
                    $"{_fixture.BaseUrl}/feed-upload")));

        // Step 2: Upload XML to pre-signed URL
        _mockServer
            .Given(Request.Create()
                .WithPath("/feed-upload")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200));

        // Step 3: Create the feed
        _mockServer
            .Given(Request.Create()
                .WithPath("/feeds/2021-06-30/feeds")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(AmazonWireMockHelper.BuildCreateFeedResponse("FD-TEST-001")));
    }

    public void Dispose()
    {
        // Fixture is shared and disposed by xUnit
    }
}
