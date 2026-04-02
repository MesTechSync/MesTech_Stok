using System.Net;
using FluentAssertions;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace MesTech.Integration.Tests.Adapters;

/// <summary>
/// EbayAdapter integration tests using WireMock.Net.
/// Each test spins up a real HTTP stub via WireMockFixture and exercises the
/// adapter end-to-end without hitting the live eBay API.
/// </summary>
public class EbayAdapterExtendedTests : IClassFixture<WireMockFixture>
{
    private readonly WireMockFixture _fixture;

    // Shared test credentials — TokenEndpoint overridden to WireMock URL
    private Dictionary<string, string> BuildCredentials() => new()
    {
        ["ClientId"] = "test-client-id",
        ["ClientSecret"] = "test-client-secret",
        ["TokenEndpoint"] = $"{_fixture.ServerUrl}/identity/v1/oauth2/token"
    };

    private const string TokenResponseJson =
        """{"access_token":"mock-ebay-token","token_type":"Application Access Token","expires_in":7200}""";

    public EbayAdapterExtendedTests(WireMockFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Builds an EbayAdapter whose HttpClient BaseAddress is NOT set —
    /// all URL calls use absolute paths constructed inside the adapter.
    /// We intercept those absolute paths via WireMock by replacing the
    /// eBay base URL with the WireMock server URL through the HttpClient
    /// DelegatingHandler approach is not needed here because the adapter
    /// constructs absolute URLs using EbayBaseUrl constant.
    ///
    /// Instead we create an HttpClient that routes through WireMock by
    /// using a custom handler that rewrites the host.
    /// </summary>
    private EbayAdapter BuildAdapter()
    {
        // The adapter hard-codes https://api.ebay.com as the base.
        // We use a rewriting handler to redirect those calls to WireMock.
        var handler = new EbayHostRewriteHandler(_fixture.ServerUrl)
        {
            InnerHandler = new HttpClientHandler()
        };
        var httpClient = new HttpClient(handler);
        var logger = new Mock<ILogger<EbayAdapter>>().Object;
        return new EbayAdapter(httpClient, logger);
    }

    private void StubTokenEndpoint()
    {
        _fixture.Server
            .Given(Request.Create()
                .WithPath("/identity/v1/oauth2/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(TokenResponseJson));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 1: TestConnection_ValidToken_ReturnsSuccess
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_ValidToken_ReturnsSuccess()
    {
        // Arrange
        _fixture.Server.Reset();
        StubTokenEndpoint();

        var adapter = BuildAdapter();
        var credentials = BuildCredentials();

        // Act
        var result = await adapter.TestConnectionAsync(credentials);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("eBay");
        result.StoreName.Should().Contain("OAuth2 OK");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 2: PullProducts_ReturnsItems
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PullProducts_ReturnsItems()
    {
        // Arrange
        _fixture.Server.Reset();
        StubTokenEndpoint();

        const string inventoryJson = """
            {
              "inventoryItems": [
                {
                  "sku": "SKU-001",
                  "product": { "title": "Widget Pro" },
                  "availability": {
                    "shipToLocationAvailability": { "quantity": 42 }
                  }
                },
                {
                  "sku": "SKU-002",
                  "product": { "title": "Gadget Ultra" },
                  "availability": {
                    "shipToLocationAvailability": { "quantity": 7 }
                  }
                }
              ],
              "total": 2
            }
            """;

        _fixture.Server
            .Given(Request.Create()
                .WithPath("/sell/inventory/v1/inventory_item")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(inventoryJson));

        var adapter = BuildAdapter();
        // Configure via TestConnection first to set credentials
        await adapter.TestConnectionAsync(BuildCredentials());

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert
        products.Should().NotBeNull();
        products.Should().HaveCount(2);
        products[0].SKU.Should().Be("SKU-001");
        products[0].Name.Should().Be("Widget Pro");
        products[0].Stock.Should().Be(42);
        products[1].SKU.Should().Be("SKU-002");
        products[1].Stock.Should().Be(7);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 3: PushStockUpdate_Success
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PushStockUpdate_Success()
    {
        // Arrange
        _fixture.Server.Reset();
        StubTokenEndpoint();

        var productId = Guid.NewGuid();
        var encodedSku = Uri.EscapeDataString(productId.ToString());
        var itemPath = $"/sell/inventory/v1/inventory_item/{encodedSku}";

        // GET existing item
        _fixture.Server
            .Given(Request.Create()
                .WithPath(itemPath)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"sku":"test","product":{"title":"Test Item"}}"""));

        // PUT updated item — eBay returns 204 No Content on success
        _fixture.Server
            .Given(Request.Create()
                .WithPath(itemPath)
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NoContent));

        var adapter = BuildAdapter();
        await adapter.TestConnectionAsync(BuildCredentials());

        // Act
        var result = await adapter.PushStockUpdateAsync(productId, 99);

        // Assert
        result.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 4: PullOrders_ReturnsOrders
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PullOrders_ReturnsOrders()
    {
        // Arrange
        _fixture.Server.Reset();
        StubTokenEndpoint();

        const string ordersJson = """
            {
              "orders": [
                {
                  "orderId": "ORD-9001",
                  "orderFulfillmentStatus": "NOT_STARTED",
                  "creationDate": "2026-03-01T10:00:00.000Z",
                  "pricingSummary": {
                    "total": { "value": "149.99", "currency": "USD" }
                  },
                  "buyer": { "username": "buyer123" },
                  "lineItems": [
                    {
                      "lineItemId": "LI-1",
                      "sku": "SKU-001",
                      "title": "Widget Pro",
                      "quantity": 3,
                      "lineItemCost": { "value": "149.97" }
                    }
                  ]
                }
              ],
              "total": 1
            }
            """;

        _fixture.Server
            .Given(Request.Create()
                .WithPath("/sell/fulfillment/v1/order")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(ordersJson));

        var adapter = BuildAdapter();
        await adapter.TestConnectionAsync(BuildCredentials());

        // Act
        var orders = await adapter.PullOrdersAsync(DateTime.UtcNow.AddDays(-7));

        // Assert
        orders.Should().NotBeNull();
        orders.Should().HaveCount(1);
        orders[0].PlatformOrderId.Should().Be("ORD-9001");
        orders[0].Status.Should().Be("NOT_STARTED");
        orders[0].TotalAmount.Should().Be(149.99m);
        orders[0].Currency.Should().Be("USD");
        orders[0].Lines.Should().HaveCount(1);
        orders[0].Lines[0].SKU.Should().Be("SKU-001");
        orders[0].Lines[0].Quantity.Should().Be(3);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 5: PlatformCode_IsEBay
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void PlatformCode_IsEBay()
    {
        // Arrange
        var adapter = BuildAdapter();

        // Act & Assert — simple property check, no HTTP calls needed
        adapter.PlatformCode.Should().Be("eBay");
        adapter.SupportsStockUpdate.Should().BeTrue();
        adapter.SupportsPriceUpdate.Should().BeTrue();
        adapter.SupportsShipment.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 6: SendShipment_Success (bonus)
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendShipment_Success()
    {
        // Arrange
        _fixture.Server.Reset();
        StubTokenEndpoint();

        const string orderId = "ORDER-5555";
        var encodedOrderId = Uri.EscapeDataString(orderId);

        _fixture.Server
            .Given(Request.Create()
                .WithPath($"/sell/fulfillment/v1/order/{encodedOrderId}/shipping_fulfillment")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Created));

        var adapter = BuildAdapter();
        await adapter.TestConnectionAsync(BuildCredentials());

        // Act
        var result = await adapter.SendShipmentAsync(orderId, "TRK123456", CargoProvider.UPS);

        // Assert
        result.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 7: TestConnection_MissingCredentials_ReturnsFailure
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_MissingCredentials_ReturnsFailure()
    {
        // Arrange — no WireMock stubs needed; adapter returns early for missing creds
        var adapter = BuildAdapter();
        var emptyCredentials = new Dictionary<string, string>
        {
            ["ClientId"] = "",
            ["ClientSecret"] = ""
        };

        // Act
        var result = await adapter.TestConnectionAsync(emptyCredentials);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}

/// <summary>
/// DelegatingHandler that rewrites the host/scheme of outgoing requests
/// from https://api.ebay.com to the WireMock server URL.
/// This lets EbayAdapter use its hard-coded absolute eBay URLs while
/// all traffic is silently redirected to the local WireMock stub.
/// </summary>
internal sealed class EbayHostRewriteHandler : DelegatingHandler
{
    private readonly Uri _targetBase;

    public EbayHostRewriteHandler(string wireMockUrl)
    {
        _targetBase = new Uri(wireMockUrl.TrimEnd('/'));
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null)
        {
            var original = request.RequestUri;
            var rewritten = new UriBuilder(original)
            {
                Scheme = _targetBase.Scheme,
                Host = _targetBase.Host,
                Port = _targetBase.Port
            }.Uri;

            request.RequestUri = rewritten;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
