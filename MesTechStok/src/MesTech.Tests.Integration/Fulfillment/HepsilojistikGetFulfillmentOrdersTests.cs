using System.Net;
using FluentAssertions;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Infrastructure.Integration.Fulfillment;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Fulfillment;

/// <summary>
/// HepsilojistikAdapter.GetFulfillmentOrdersAsync contract tests.
/// Covers happy path, error response, empty result, and pagination.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Adapter", "Hepsilojistik")]
public class HepsilojistikGetFulfillmentOrdersTests
{
    private const string TestMerchantId = "MERCHANT-HL-001";
    private const string TestApiKey = "test-api-key-hl-secret";

    private static HepsilojistikAdapter CreateAdapter(WireMockServer server)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(server.Url!) };
        var logger = new LoggerFactory().CreateLogger<HepsilojistikAdapter>();
        return new HepsilojistikAdapter(httpClient, logger, TestMerchantId, TestApiKey);
    }

    // ── 1. Happy path — returns orders with items ──

    [Fact]
    public async Task GetFulfillmentOrders_Success_ReturnsOrdersWithItems()
    {
        // Arrange
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/orders")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""orders"": [
                        {
                            ""orderId"": ""HL-ORD-001"",
                            ""status"": ""SHIPPED"",
                            ""shippedDate"": ""2026-03-18T10:00:00Z"",
                            ""trackingNumber"": ""TR12345678"",
                            ""carrierName"": ""Aras Kargo"",
                            ""items"": [
                                {""merchantSku"": ""SKU-A"", ""quantity"": 2, ""quantityShipped"": 2},
                                {""merchantSku"": ""SKU-B"", ""quantity"": 5, ""quantityShipped"": 3}
                            ]
                        },
                        {
                            ""orderId"": ""HL-ORD-002"",
                            ""status"": ""PROCESSING"",
                            ""items"": [
                                {""merchantSku"": ""SKU-C"", ""quantity"": 10, ""quantityShipped"": 0}
                            ]
                        }
                    ]
                }")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.GetFulfillmentOrdersAsync(DateTime.UtcNow.AddDays(-7));

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var order1 = result.First(o => o.OrderId == "HL-ORD-001");
        order1.Status.Should().Be("SHIPPED");
        order1.ShippedDate.Should().NotBeNull();
        order1.TrackingNumber.Should().Be("TR12345678");
        order1.CarrierName.Should().Be("Aras Kargo");
        order1.Items.Should().HaveCount(2);
        order1.Items.First(i => i.SKU == "SKU-A").QuantityShipped.Should().Be(2);
        order1.Items.First(i => i.SKU == "SKU-B").QuantityShipped.Should().Be(3);

        var order2 = result.First(o => o.OrderId == "HL-ORD-002");
        order2.Status.Should().Be("PROCESSING");
    }

    // ── 2. Error response — API returns 400 ──

    [Fact]
    public async Task GetFulfillmentOrders_ApiError_ReturnsEmptyList()
    {
        // Arrange
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/orders")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""BAD_REQUEST"",""detail"":""Invalid date range""}")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.GetFulfillmentOrdersAsync(DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty("400 error should result in empty order list");
    }

    // ── 3. Empty result — no orders ──

    [Fact]
    public async Task GetFulfillmentOrders_NoOrders_ReturnsEmptyList()
    {
        // Arrange
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/orders")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""orders"": []}")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.GetFulfillmentOrdersAsync(DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty("empty orders array should result in empty list");
    }

    // ── 4. Null shipment id in order — skipped ──

    [Fact]
    public async Task GetFulfillmentOrders_MissingOrderId_IsSkipped()
    {
        // Arrange — one order has orderId, one does not
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/orders")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""orders"": [
                        {
                            ""orderId"": ""HL-ORD-VALID"",
                            ""status"": ""SHIPPED"",
                            ""items"": [{""merchantSku"": ""SKU-X"", ""quantity"": 1}]
                        },
                        {
                            ""status"": ""UNKNOWN"",
                            ""items"": [{""merchantSku"": ""SKU-Y"", ""quantity"": 2}]
                        }
                    ]
                }")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.GetFulfillmentOrdersAsync(DateTime.UtcNow.AddDays(-7));

        // Assert — order without orderId is skipped
        result.Should().ContainSingle();
        result.First().OrderId.Should().Be("HL-ORD-VALID");
    }

    // ── 5. Alternative JSON shape: "data" array instead of "orders" ──

    [Fact]
    public async Task GetFulfillmentOrders_DataArrayShape_ParsesCorrectly()
    {
        // Arrange — adapter handles both "orders" and "data" property names
        using var server = WireMockServer.Start();
        server.Given(
            Request.Create()
                .WithPath("/orders")
                .UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"": [
                        {
                            ""orderId"": ""HL-DATA-001"",
                            ""status"": ""DELIVERED"",
                            ""items"": [{""merchantSku"": ""SKU-DATA"", ""quantity"": 3}]
                        }
                    ]
                }")
        );

        var adapter = CreateAdapter(server);

        // Act
        var result = await adapter.GetFulfillmentOrdersAsync(DateTime.UtcNow.AddDays(-1));

        // Assert
        result.Should().ContainSingle();
        result.First().OrderId.Should().Be("HL-DATA-001");
        result.First().Status.Should().Be("DELIVERED");
    }

    // ── 6. GetInboundStatus — null/whitespace throws ArgumentException ──

    [Fact]
    public async Task GetInboundStatus_NullShipmentId_ThrowsArgumentException()
    {
        using var server = WireMockServer.Start();
        var adapter = CreateAdapter(server);

        var act = () => adapter.GetInboundStatusAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();

        server.LogEntries.Should().BeEmpty("no HTTP call should be made for null input");
    }

    // ── 7. GetInventoryLevels — null skus throws ArgumentNullException ──

    [Fact]
    public async Task GetInventoryLevels_NullSkus_ThrowsArgumentNullException()
    {
        using var server = WireMockServer.Start();
        var adapter = CreateAdapter(server);

        var act = () => adapter.GetInventoryLevelsAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
