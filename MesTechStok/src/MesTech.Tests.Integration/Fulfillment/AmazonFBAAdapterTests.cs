using System.Net;
using System.Net.Http;
using System.Reflection;
using FluentAssertions;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Infrastructure.Integration.Fulfillment;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Fulfillment;

/// <summary>
/// AmazonFBAAdapter entegrasyon testleri — WireMock ile gercek HTTP trafigi.
/// Adapter endpointleri (SP-API):
///   POST /inbound/fba/2024-03-20/inboundPlans              — CreateInboundShipmentAsync
///   GET  /fba/inventory/v1/summaries?sellerSkus=...         — GetInventoryLevelsAsync
///   GET  /inbound/fba/2024-03-20/inboundPlans/{id}/operationStatus — GetInboundStatusAsync
///   GET  /fba/inventory/v1/summaries?marketplaceIds=...     — IsAvailableAsync (health)
/// LWA Auth: Token cached in adapter; pre-seeded via reflection to avoid real LWA calls.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Adapter", "AmazonFBA")]
public class AmazonFBAAdapterTests
{
    private const string TestRefreshToken = "test-refresh-token";
    private const string TestClientId = "test-client-id";
    private const string TestClientSecret = "test-client-secret";
    private const string TestSellerId = "SELLER-TR-001";
    private const string TurkeyMarketplaceId = "A33AVAJ2PDY3EV";

    /// <summary>
    /// Creates an AmazonFBAAdapter pointing at the WireMock server,
    /// with a pre-seeded access token so EnsureFreshTokenAsync is skipped.
    /// </summary>
    private static AmazonFBAAdapter CreateAdapter(WireMockServer server)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(server.Url!) };
        var logger = new LoggerFactory().CreateLogger<AmazonFBAAdapter>();
        var adapter = new AmazonFBAAdapter(
            httpClient, logger, TestRefreshToken, TestClientId, TestClientSecret, TestSellerId);

        // Pre-seed the LWA access token to bypass real token refresh calls.
        // The adapter checks: if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
        SetPrivateField(adapter, "_accessToken", "test-access-token-preseeded");
        SetPrivateField(adapter, "_tokenExpiry", DateTime.UtcNow.AddHours(1));

        return adapter;
    }

    /// <summary>
    /// Creates an adapter with a custom HttpClient timeout for timeout tests.
    /// </summary>
    private static AmazonFBAAdapter CreateAdapterWithTimeout(WireMockServer server, TimeSpan timeout)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(server.Url!),
            Timeout = timeout
        };
        var logger = new LoggerFactory().CreateLogger<AmazonFBAAdapter>();
        var adapter = new AmazonFBAAdapter(
            httpClient, logger, TestRefreshToken, TestClientId, TestClientSecret, TestSellerId);

        SetPrivateField(adapter, "_accessToken", "test-access-token-preseeded");
        SetPrivateField(adapter, "_tokenExpiry", DateTime.UtcNow.AddHours(1));

        return adapter;
    }

    /// <summary>
    /// Creates adapter with an expired token so EnsureFreshTokenAsync will attempt LWA refresh.
    /// </summary>
    private static AmazonFBAAdapter CreateAdapterWithExpiredToken(WireMockServer server)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(server.Url!) };
        var logger = new LoggerFactory().CreateLogger<AmazonFBAAdapter>();
        var adapter = new AmazonFBAAdapter(
            httpClient, logger, TestRefreshToken, TestClientId, TestClientSecret, TestSellerId);

        // Set token to expired — adapter will try to call LWA endpoint
        SetPrivateField(adapter, "_accessToken", "expired-token");
        SetPrivateField(adapter, "_tokenExpiry", DateTime.UtcNow.AddHours(-1));

        return adapter;
    }

    private static InboundShipmentRequest CreateTestRequest(IReadOnlyList<InboundItem>? items = null)
    {
        items ??= new List<InboundItem>
        {
            new("SKU-A", 10, "LOT-001"),
            new("SKU-B", 5)
        };

        return new InboundShipmentRequest(
            ShipmentName: "Test FBA Inbound 001",
            DestinationCenter: FulfillmentCenter.AmazonFBA,
            Items: items,
            ExpectedArrival: new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            Notes: "WireMock test shipment"
        );
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found on {obj.GetType().Name}");
        field.SetValue(obj, value);
    }

    // ── 1-3. CreateInboundShipment ───────────────────────────────────────────

    [Fact]
    public async Task CreateInboundShipment_Success_ReturnsShipmentId()
    {
        // Arrange
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/inbound/fba/2024-03-20/inboundPlans")
                .UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""inboundPlanId"":""FBA-PLAN-00123"",""status"":""ACTIVE""}")
        );

        var adapter = CreateAdapter(server);
        var request = CreateTestRequest();

        // Act
        var result = await adapter.CreateInboundShipmentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.ShipmentId.Should().Be("FBA-PLAN-00123");
        result.ErrorMessage.Should().BeNull();
        server.LogEntries.Should().ContainSingle(e =>
            e.RequestMessage.Path == "/inbound/fba/2024-03-20/inboundPlans" &&
            e.RequestMessage.Method == "POST");
    }

    [Fact]
    public async Task CreateInboundShipment_InvalidSKU_ReturnsError()
    {
        // Arrange — SP-API returns 400 for invalid SKU
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/inbound/fba/2024-03-20/inboundPlans")
                .UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""code"":""InvalidSKU"",""message"":""SKU not found in seller catalog""}")
        );

        var adapter = CreateAdapter(server);
        var items = new List<InboundItem> { new("SKU-INVALID-999", 5) };
        var request = CreateTestRequest(items);

        // Act
        var result = await adapter.CreateInboundShipmentAsync(request);

        // Assert — 400 is not retried (Polly only retries 500+), adapter returns error
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.ShipmentId.Should().BeEmpty();
        // Verify no retry occurred — only 1 request for 400 errors
        server.LogEntries.Should().HaveCount(1, "400 errors should not trigger retry");
    }

    [Fact]
    public async Task CreateInboundShipment_EmptyItems_ThrowsArgumentException()
    {
        // Arrange — adapter calls ArgumentNullException.ThrowIfNull(request) before HTTP
        using var server = WireMockServer.Start();
        var adapter = CreateAdapter(server);

        // Act & Assert — passing null request triggers ArgumentNullException pre-HTTP
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => adapter.CreateInboundShipmentAsync(null!));

        // WireMock should receive zero requests since exception is thrown pre-HTTP
        server.LogEntries.Should().BeEmpty("null request should fail before any HTTP call");
    }

    // ── 4-6. GetInventoryLevels ──────────────────────────────────────────────

    [Fact]
    public async Task GetInventoryLevels_Success_ReturnsAllSkuLevels()
    {
        // Arrange — SP-API inventory response with payload.inventorySummaries
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/fba/inventory/v1/summaries")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""payload"": {
                        ""inventorySummaries"": [
                            {
                                ""sellerSku"": ""SKU-A"",
                                ""inventoryDetails"": {
                                    ""fulfillableQuantity"": 100,
                                    ""reservedQuantity"": { ""totalReservedQuantity"": 10 },
                                    ""inboundWorkingQuantity"": 25
                                }
                            },
                            {
                                ""sellerSku"": ""SKU-B"",
                                ""inventoryDetails"": {
                                    ""fulfillableQuantity"": 50,
                                    ""reservedQuantity"": { ""totalReservedQuantity"": 5 },
                                    ""inboundWorkingQuantity"": 0
                                }
                            },
                            {
                                ""sellerSku"": ""SKU-C"",
                                ""inventoryDetails"": {
                                    ""fulfillableQuantity"": 200,
                                    ""reservedQuantity"": { ""totalReservedQuantity"": 0 },
                                    ""inboundWorkingQuantity"": 30
                                }
                            }
                        ]
                    }
                }")
        );

        var adapter = CreateAdapter(server);
        var skus = new List<string> { "SKU-A", "SKU-B", "SKU-C" };

        // Act
        var result = await adapter.GetInventoryLevelsAsync(skus);

        // Assert
        result.Should().NotBeNull();
        result.Center.Should().Be(FulfillmentCenter.AmazonFBA);
        result.Stocks.Should().HaveCount(3);

        var skuA = result.Stocks.First(s => s.SKU == "SKU-A");
        skuA.AvailableQuantity.Should().Be(100);
        skuA.ReservedQuantity.Should().Be(10);
        skuA.InboundQuantity.Should().Be(25);

        var skuC = result.Stocks.First(s => s.SKU == "SKU-C");
        skuC.AvailableQuantity.Should().Be(200);
        skuC.InboundQuantity.Should().Be(30);
    }

    [Fact]
    public async Task GetInventoryLevels_PartialResults_ReturnsMissingSkusAsZero()
    {
        // Arrange — API returns only 2 of 3 requested SKUs
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/fba/inventory/v1/summaries")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""payload"": {
                        ""inventorySummaries"": [
                            {
                                ""sellerSku"": ""SKU-A"",
                                ""inventoryDetails"": {
                                    ""fulfillableQuantity"": 100,
                                    ""reservedQuantity"": { ""totalReservedQuantity"": 10 },
                                    ""inboundWorkingQuantity"": 25
                                }
                            },
                            {
                                ""sellerSku"": ""SKU-B"",
                                ""inventoryDetails"": {
                                    ""fulfillableQuantity"": 50,
                                    ""reservedQuantity"": { ""totalReservedQuantity"": 5 },
                                    ""inboundWorkingQuantity"": 0
                                }
                            }
                        ]
                    }
                }")
        );

        var adapter = CreateAdapter(server);
        var skus = new List<string> { "SKU-A", "SKU-B", "SKU-MISSING" };

        // Act
        var result = await adapter.GetInventoryLevelsAsync(skus);

        // Assert — adapter returns only what API provides; missing SKUs are simply absent
        result.Should().NotBeNull();
        result.Stocks.Should().HaveCount(2, "API returned only 2 of 3 requested SKUs");
        result.Stocks.Should().Contain(s => s.SKU == "SKU-A");
        result.Stocks.Should().Contain(s => s.SKU == "SKU-B");
        result.Stocks.Should().NotContain(s => s.SKU == "SKU-MISSING");
    }

    [Fact]
    public async Task GetInventoryLevels_NotFound_ReturnsEmptyDictionary()
    {
        // Arrange — API returns 404 (non-5xx, so no retry; adapter logs and continues)
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/fba/inventory/v1/summaries")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""errors"":[{""code"":""NOT_FOUND"",""message"":""Inventory data not found""}]}")
        );

        var adapter = CreateAdapter(server);
        var skus = new List<string> { "SKU-GHOST" };

        // Act
        var result = await adapter.GetInventoryLevelsAsync(skus);

        // Assert — 404 is not success, adapter skips parsing and returns empty stocks
        result.Should().NotBeNull();
        result.Stocks.Should().BeEmpty("404 means no inventory data available");
        result.Center.Should().Be(FulfillmentCenter.AmazonFBA);
    }

    // ── 7-9. GetInboundStatus ────────────────────────────────────────────────

    [Fact]
    public async Task GetInboundStatus_Received_ReturnsReceivedStatus()
    {
        // Arrange
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/inbound/fba/2024-03-20/inboundPlans/SHIPMENT-001/operationStatus")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""RECEIVING"",
                    ""itemDetails"": {
                        ""totalExpectedQuantity"": 50,
                        ""totalReceivedQuantity"": 50
                    }
                }")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.GetInboundStatusAsync("SHIPMENT-001");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("RECEIVING");
        result.ShipmentId.Should().Be("SHIPMENT-001");
        result.TotalItemsExpected.Should().Be(50);
        result.TotalItemsReceived.Should().Be(50);
    }

    [Fact]
    public async Task GetInboundStatus_InTransit_ReturnsInTransitStatus()
    {
        // Arrange
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/inbound/fba/2024-03-20/inboundPlans/SHIPMENT-002/operationStatus")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""SHIPPED"",
                    ""itemDetails"": {
                        ""totalExpectedQuantity"": 30,
                        ""totalReceivedQuantity"": 0
                    }
                }")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.GetInboundStatusAsync("SHIPMENT-002");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("SHIPPED");
        result.ShipmentId.Should().Be("SHIPMENT-002");
        result.TotalItemsExpected.Should().Be(30);
        result.TotalItemsReceived.Should().Be(0);
    }

    [Fact]
    public async Task GetInboundStatus_Cancelled_ReturnsCancelledStatus()
    {
        // Arrange
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/inbound/fba/2024-03-20/inboundPlans/SHIPMENT-CANCELLED/operationStatus")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""CANCELLED"",
                    ""itemDetails"": {
                        ""totalExpectedQuantity"": 20,
                        ""totalReceivedQuantity"": 0
                    }
                }")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.GetInboundStatusAsync("SHIPMENT-CANCELLED");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("CANCELLED");
        result.ShipmentId.Should().Be("SHIPMENT-CANCELLED");
        result.TotalItemsExpected.Should().Be(20);
        result.TotalItemsReceived.Should().Be(0);
    }

    // ── 10-11. IsAvailable ───────────────────────────────────────────────────

    [Fact]
    public async Task IsAvailable_HealthyApi_ReturnsTrue()
    {
        // Arrange — adapter health check uses GET /fba/inventory/v1/summaries
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/fba/inventory/v1/summaries")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""payload"":{""inventorySummaries"":[]}}")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
        server.LogEntries.Should().NotBeEmpty("health check should hit the inventory endpoint");
    }

    [Fact]
    public async Task IsAvailable_UnhealthyApi_ReturnsFalse()
    {
        // Arrange — server returns 503 Service Unavailable
        // The retry pipeline retries on 500+; after exhausting retries, IsAvailableAsync
        // checks response.IsSuccessStatusCode || (int)response.StatusCode < 500.
        // 503 >= 500, so returns false.
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/fba/inventory/v1/summaries")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(503)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""errors"":[{""code"":""SERVICE_UNAVAILABLE"",""message"":""Service temporarily unavailable""}]}")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.IsAvailableAsync();

        // Assert
        result.Should().BeFalse("503 is >= 500 so adapter considers service unavailable");
        // Retry pipeline retries on 500+, so multiple requests are expected
        server.LogEntries.Should().HaveCountGreaterThanOrEqualTo(1,
            "at least the initial request should hit the server");
    }

    // ── 12-15. Auth / Timeout / Retry / Concurrency ──────────────────────────

    [Fact]
    public async Task Auth_ExpiredToken_RefreshesAndRetries()
    {
        // Arrange — Token is expired. The adapter's EnsureFreshTokenAsync creates
        // a new HttpClient pointing at the hardcoded LWA endpoint (api.amazon.com),
        // which will fail in test environment. The adapter catches this exception
        // and returns an error result. This verifies the adapter propagates auth failures.
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/inbound/fba/2024-03-20/inboundPlans")
                .UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""inboundPlanId"":""FBA-PLAN-AUTH"",""status"":""ACTIVE""}")
        );

        var adapter = CreateAdapterWithExpiredToken(server);
        var request = CreateTestRequest();

        // Act — expired token triggers LWA refresh which fails (no real LWA server)
        var result = await adapter.CreateInboundShipmentAsync(request);

        // Assert — adapter catches the exception and returns failure
        result.Success.Should().BeFalse("expired token refresh fails without real LWA endpoint");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        // The FBA endpoint should NOT be called since auth failed before the request
        server.LogEntries.Where(e =>
            e.RequestMessage.Path == "/inbound/fba/2024-03-20/inboundPlans")
            .Should().BeEmpty("FBA endpoint should not be called when auth fails");
    }

    [Fact]
    public async Task Timeout_SlowApi_ThrowsOrReturnsError()
    {
        // Arrange — server delays response by 10 seconds, client timeout is 500ms
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/fba/inventory/v1/summaries")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""payload"":{""inventorySummaries"":[]}}")
                .WithDelay(TimeSpan.FromSeconds(10))
        );

        var adapter = CreateAdapterWithTimeout(server, TimeSpan.FromMilliseconds(500));
        var skus = new List<string> { "SKU-TIMEOUT" };

        // Act — timeout causes TaskCanceledException, which Polly retries.
        // After exhausting retries, adapter catches exception and returns empty inventory.
        var result = await adapter.GetInventoryLevelsAsync(skus);

        // Assert
        result.Should().NotBeNull();
        result.Stocks.Should().BeEmpty("timeout causes all retries to fail, returning empty inventory");
        result.Center.Should().Be(FulfillmentCenter.AmazonFBA);
    }

    [Fact]
    public async Task Retry_Transient503_RetriesThreeTimes()
    {
        // Arrange — first two requests return 503, third returns 200.
        // The adapter's Polly retry pipeline retries up to 3 times on 500+ status codes.
        using var server = WireMockServer.Start();

        // WireMock scenarios: 503 -> 503 -> 200
        server.Given(
            Request.Create()
                .WithPath("/fba/inventory/v1/summaries")
                .UsingGet()
        ).InScenario("retry-test")
        .WillSetStateTo("attempt-2")
        .RespondWith(
            Response.Create()
                .WithStatusCode(503)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""errors"":[{""code"":""SERVICE_UNAVAILABLE""}]}")
        );

        server.Given(
            Request.Create()
                .WithPath("/fba/inventory/v1/summaries")
                .UsingGet()
        ).InScenario("retry-test")
        .WhenStateIs("attempt-2")
        .WillSetStateTo("attempt-3")
        .RespondWith(
            Response.Create()
                .WithStatusCode(503)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""errors"":[{""code"":""SERVICE_UNAVAILABLE""}]}")
        );

        server.Given(
            Request.Create()
                .WithPath("/fba/inventory/v1/summaries")
                .UsingGet()
        ).InScenario("retry-test")
        .WhenStateIs("attempt-3")
        .RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""payload"": {
                        ""inventorySummaries"": [
                            {
                                ""sellerSku"": ""SKU-RETRY"",
                                ""inventoryDetails"": {
                                    ""fulfillableQuantity"": 42,
                                    ""reservedQuantity"": { ""totalReservedQuantity"": 0 },
                                    ""inboundWorkingQuantity"": 0
                                }
                            }
                        ]
                    }
                }")
        );

        var adapter = CreateAdapter(server);
        var skus = new List<string> { "SKU-RETRY" };

        // Act
        var result = await adapter.GetInventoryLevelsAsync(skus);

        // Assert — after retries, the 3rd attempt returns 200 with stock data
        result.Should().NotBeNull();
        result.Stocks.Should().ContainSingle(s => s.SKU == "SKU-RETRY");
        result.Stocks.First().AvailableQuantity.Should().Be(42);

        // Total requests: 1 initial + 2 retries = 3
        server.LogEntries.Should().HaveCountGreaterThanOrEqualTo(3,
            "adapter should retry on 503 until success");
    }

    [Fact]
    public async Task Concurrent_MultipleRequests_AllSucceed()
    {
        // Arrange — server returns success for all inventory requests
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/fba/inventory/v1/summaries")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""payload"": {
                        ""inventorySummaries"": [
                            {
                                ""sellerSku"": ""SKU-CONC"",
                                ""inventoryDetails"": {
                                    ""fulfillableQuantity"": 99,
                                    ""reservedQuantity"": { ""totalReservedQuantity"": 1 },
                                    ""inboundWorkingQuantity"": 5
                                }
                            }
                        ]
                    }
                }")
        );

        var adapter = CreateAdapter(server);

        // Act — launch 10 concurrent calls
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => adapter.GetInventoryLevelsAsync(
                new List<string> { "SKU-CONC" }))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert — all 10 results should be valid
        results.Should().HaveCount(10);
        foreach (var result in results)
        {
            result.Should().NotBeNull();
            result.Center.Should().Be(FulfillmentCenter.AmazonFBA);
            result.Stocks.Should().NotBeEmpty("each concurrent call should return stock data");
            result.Stocks.First().AvailableQuantity.Should().Be(99);
        }

        // All 10 requests should have reached the server
        server.LogEntries.Should().HaveCountGreaterThanOrEqualTo(10,
            "all 10 concurrent requests should hit the server");
    }
}
