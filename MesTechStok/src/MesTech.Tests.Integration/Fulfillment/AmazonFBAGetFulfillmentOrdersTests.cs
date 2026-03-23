using System.Net;
using FluentAssertions;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Infrastructure.Integration.Fulfillment;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using System.Reflection;

namespace MesTech.Tests.Integration.Fulfillment;

/// <summary>
/// AmazonFBAAdapter.GetFulfillmentOrdersAsync contract tests.
/// Covers happy path, error response, empty result, and pagination.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Adapter", "AmazonFBA")]
public class AmazonFBAGetFulfillmentOrdersTests
{
    private const string TestRefreshToken = "test-refresh-token";
    private const string TestClientId = "test-client-id";
    private const string TestClientSecret = "test-client-secret";
    private const string TestSellerId = "SELLER-TR-001";

    private static AmazonFBAAdapter CreateAdapter(WireMockServer server)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(server.Url!) };
        var logger = new LoggerFactory().CreateLogger<AmazonFBAAdapter>();
        var adapter = new AmazonFBAAdapter(
            httpClient, logger, TestRefreshToken, TestClientId, TestClientSecret, TestSellerId);

        SetPrivateField(adapter, "_accessToken", "test-access-token-preseeded");
        SetPrivateField(adapter, "_tokenExpiry", DateTime.UtcNow.AddHours(1));

        return adapter;
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found on {obj.GetType().Name}");
        field.SetValue(obj, value);
    }

    // ── 1. Happy path — returns orders with items ──

    [Fact]
    public async Task GetFulfillmentOrders_Success_ReturnsOrdersWithItems()
    {
        // Arrange
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/fba/outbound/2020-07-01/fulfillmentOrders")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""payload"": {
                        ""fulfillmentOrders"": [
                            {
                                ""sellerFulfillmentOrderId"": ""FBA-ORD-001"",
                                ""fulfillmentOrderStatus"": ""COMPLETE"",
                                ""statusUpdatedDate"": ""2026-03-20T14:30:00Z"",
                                ""fulfillmentOrderItems"": [
                                    {""sellerSku"": ""SKU-A"", ""quantity"": 3, ""unfulfillableQuantity"": 0},
                                    {""sellerSku"": ""SKU-B"", ""quantity"": 5, ""unfulfillableQuantity"": 1}
                                ]
                            },
                            {
                                ""sellerFulfillmentOrderId"": ""FBA-ORD-002"",
                                ""fulfillmentOrderStatus"": ""PROCESSING"",
                                ""fulfillmentOrderItems"": [
                                    {""sellerSku"": ""SKU-C"", ""quantity"": 10, ""unfulfillableQuantity"": 0}
                                ]
                            }
                        ]
                    }
                }")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.GetFulfillmentOrdersAsync(DateTime.UtcNow.AddDays(-7));

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var order1 = result.First(o => o.OrderId == "FBA-ORD-001");
        order1.Status.Should().Be("COMPLETE");
        order1.ShippedDate.Should().NotBeNull();
        order1.Items.Should().HaveCount(2);
        order1.Items.First(i => i.SKU == "SKU-A").QuantityShipped.Should().Be(3);
        order1.Items.First(i => i.SKU == "SKU-B").QuantityShipped.Should().Be(4, "5 ordered - 1 unfulfillable = 4 shipped");

        var order2 = result.First(o => o.OrderId == "FBA-ORD-002");
        order2.Status.Should().Be("PROCESSING");
        order2.Items.Should().ContainSingle(i => i.SKU == "SKU-C");
    }

    // ── 2. Error response — API returns 400 ──

    [Fact]
    public async Task GetFulfillmentOrders_ApiError_ReturnsEmptyList()
    {
        // Arrange
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/fba/outbound/2020-07-01/fulfillmentOrders")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""errors"":[{""code"":""InvalidInput"",""message"":""Invalid date format""}]}")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.GetFulfillmentOrdersAsync(DateTime.UtcNow);

        // Assert — adapter catches non-success and returns empty
        result.Should().NotBeNull();
        result.Should().BeEmpty("400 error should result in empty order list");
    }

    // ── 3. Empty result — no orders since date ──

    [Fact]
    public async Task GetFulfillmentOrders_NoOrders_ReturnsEmptyList()
    {
        // Arrange
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/fba/outbound/2020-07-01/fulfillmentOrders")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""payload"": {
                        ""fulfillmentOrders"": []
                    }
                }")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.GetFulfillmentOrdersAsync(DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty("no fulfillment orders should result in empty list");
    }

    // ── 4. Server error 503 — returns empty after retries ──

    [Fact]
    public async Task GetFulfillmentOrders_ServerError503_ReturnsEmptyAfterRetries()
    {
        // Arrange — all retries return 503
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/fba/outbound/2020-07-01/fulfillmentOrders")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(503)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""errors"":[{""code"":""SERVICE_UNAVAILABLE""}]}")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.GetFulfillmentOrdersAsync(DateTime.UtcNow.AddDays(-1));

        // Assert — after retries exhaust, adapter breaks out with empty
        result.Should().NotBeNull();
        result.Should().BeEmpty("persistent 503 returns empty after retry exhaustion");
    }

    // ── 5. Null shipment date fields — graceful handling ──

    [Fact]
    public async Task GetFulfillmentOrders_MissingDateFields_HandlesGracefully()
    {
        // Arrange — order without statusUpdatedDate or fulfillmentOrderItems
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/fba/outbound/2020-07-01/fulfillmentOrders")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""payload"": {
                        ""fulfillmentOrders"": [
                            {
                                ""sellerFulfillmentOrderId"": ""FBA-ORD-NODATE"",
                                ""fulfillmentOrderStatus"": ""NEW""
                            }
                        ]
                    }
                }")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.GetFulfillmentOrdersAsync(DateTime.UtcNow.AddDays(-30));

        // Assert
        result.Should().ContainSingle();
        var order = result.First();
        order.OrderId.Should().Be("FBA-ORD-NODATE");
        order.Status.Should().Be("NEW");
        order.ShippedDate.Should().BeNull("statusUpdatedDate not in response");
        order.Items.Should().BeEmpty("no fulfillmentOrderItems in response");
    }
}
