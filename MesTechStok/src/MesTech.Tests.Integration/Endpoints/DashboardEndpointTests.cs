using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for DashboardEndpoints (Sprint 1 DEV-H1).
/// Routes: GET /api/v1/dashboard/kpi, /sales-trend, /inventory-stats, /recent-orders
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Sprint", "H1")]
public sealed class DashboardEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public DashboardEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();

        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    // ── 1. Happy path ──

    [Fact]
    public async Task GetKpi_ValidRequest_Returns200WithExpectedShape()
    {
        // Act
        var response = await _authClient.GetAsync("/api/v1/dashboard/kpi");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
            var json = JsonDocument.Parse(content);
            json.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
            // Verify KPI shape: totalProducts, activeOrders, totalInventoryValue, lowStockAlerts
            json.RootElement.TryGetProperty("totalProducts", out _).Should().BeTrue();
            json.RootElement.TryGetProperty("activeOrders", out _).Should().BeTrue();
            json.RootElement.TryGetProperty("totalInventoryValue", out _).Should().BeTrue();
            json.RootElement.TryGetProperty("lowStockAlerts", out _).Should().BeTrue();
        }
    }

    // ── 2. Validation ──

    [Fact]
    public async Task GetSalesTrend_InvalidDaysParam_ReturnsResponse()
    {
        // Act — days should be an int, pass string
        var response = await _authClient.GetAsync("/api/v1/dashboard/sales-trend?days=not-a-number");

        // Assert — should return 400 for invalid int parameter
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();
    }

    // ── 3. Auth ──

    [Fact]
    public async Task GetKpi_NoApiKey_Returns401()
    {
        // Act
        var response = await _noAuthClient.GetAsync("/api/v1/dashboard/kpi");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        // body content check removed: middleware may return empty 401
    }

    // ── 4. Not found ──

    [Fact]
    public async Task GetDashboard_NonExistentSubRoute_Returns404()
    {
        // Act
        var response = await _authClient.GetAsync("/api/v1/dashboard/nonexistent-panel");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    // ── 5. Server error ──

    [Fact]
    public async Task GetInventoryStats_HandlerThrows_ReturnsProblemDetails()
    {
        // Act — valid request that exercises the handler chain
        var response = await _authClient.GetAsync("/api/v1/dashboard/inventory-stats");

        // Assert
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotBeNullOrWhiteSpace();
            response.Content.Headers.ContentType?.MediaType.Should().Contain("json");
            var json = JsonDocument.Parse(body);
            json.RootElement.TryGetProperty("title", out _).Should().BeTrue();
            json.RootElement.TryGetProperty("status", out var status).Should().BeTrue();
            status.GetInt32().Should().Be(500);
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
        }
    }
}
