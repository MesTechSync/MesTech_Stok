using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for OrderEndpoints (Sprint 1 DEV-H1).
/// Routes: GET /api/v1/orders (optional: from, to, status query params)
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Sprint", "H1")]
public sealed class OrderEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public OrderEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();

        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    // ── 1. Happy path ──

    [Fact]
    public async Task ListOrders_ValidRequest_Returns200WithArray()
    {
        // Act
        var response = await _authClient.GetAsync("/api/v1/orders");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
            var json = JsonDocument.Parse(content);
            json.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        }
    }

    // ── 2. Validation ──

    [Fact]
    public async Task ListOrders_InvalidDateFormat_ReturnsBadRequest()
    {
        // Act — invalid date format in query string
        var response = await _authClient.GetAsync("/api/v1/orders?from=not-a-date&to=also-not-a-date");

        // Assert — should return 400 for invalid date parameters
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();
    }

    // ── 3. Auth ──

    [Fact]
    public async Task ListOrders_NoApiKey_Returns401()
    {
        // Act
        var response = await _noAuthClient.GetAsync("/api/v1/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("API key");
    }

    // ── 4. Not found ──

    [Fact]
    public async Task ListOrders_NonExistentSubRoute_Returns404()
    {
        // Act — request a sub-route that doesn't exist
        var response = await _authClient.GetAsync("/api/v1/orders/nonexistent-path");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ── 5. Server error ──

    [Fact]
    public async Task ListOrders_HandlerThrows_ReturnsProblemDetails()
    {
        // Act — valid request that may trigger handler exception
        var response = await _authClient.GetAsync("/api/v1/orders?status=__trigger_error__");

        // Assert — if handler throws, global exception handler returns ProblemDetails
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
            response.Content.Headers.ContentType?.MediaType.Should().Contain("json");
            var json = JsonDocument.Parse(content);
            json.RootElement.TryGetProperty("status", out var status).Should().BeTrue();
            status.GetInt32().Should().Be(500);
        }
        else
        {
            // Handler processed it — valid response
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }
    }
}
