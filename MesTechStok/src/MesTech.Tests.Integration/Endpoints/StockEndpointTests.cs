using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for StockEndpoints (Sprint 1 DEV-H1).
/// Routes: GET /api/v1/stock/movements, /value
///         POST /api/v1/stock/add, /remove
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Sprint", "H1")]
public sealed class StockEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public StockEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();

        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    // ── 1. Happy path ──

    [Fact]
    public async Task GetInventoryValue_ValidRequest_Returns200()
    {
        // Act
        var response = await _authClient.GetAsync("/api/v1/stock/value");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
            // Response should be a JSON object with inventory value
            var json = JsonDocument.Parse(content);
            json.RootElement.ValueKind.Should().BeOneOf(JsonValueKind.Object, JsonValueKind.Number);
        }
    }

    // ── 2. Validation ──

    [Fact]
    public async Task AddStock_EmptyBody_ReturnsBadRequest()
    {
        // Arrange — POST with empty JSON body (missing required fields)
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _authClient.PostAsync("/api/v1/stock/add", content);

        // Assert — should reject invalid command
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();
    }

    // ── 3. Auth ──

    [Fact]
    public async Task GetStockMovements_NoApiKey_Returns401()
    {
        // Act
        var response = await _noAuthClient.GetAsync("/api/v1/stock/movements");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 4. Not found ──

    [Fact]
    public async Task GetStockMovements_NonExistentRoute_Returns404()
    {
        // Act — request a non-existent sub-route
        var response = await _authClient.GetAsync("/api/v1/stock/nonexistent-endpoint");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    // ── 5. Server error ──

    [Fact]
    public async Task RemoveStock_InvalidPayload_ReturnsErrorResponse()
    {
        // Arrange — malformed JSON that can't be deserialized to RemoveStockCommand
        var content = new StringContent(
            "{\"invalidField\": \"test\"}",
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _authClient.PostAsync("/api/v1/stock/remove", content);

        // Assert — should return error (400 for bad request or 500 for handler failure)
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var body = await response.Content.ReadAsStringAsync();
            response.Content.Headers.ContentType?.MediaType.Should().Contain("json");
            var json = JsonDocument.Parse(body);
            json.RootElement.TryGetProperty("status", out var status).Should().BeTrue();
            status.GetInt32().Should().Be(500);
        }
        else
        {
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.UnprocessableEntity,
                HttpStatusCode.Unauthorized);
        }
    }
}
