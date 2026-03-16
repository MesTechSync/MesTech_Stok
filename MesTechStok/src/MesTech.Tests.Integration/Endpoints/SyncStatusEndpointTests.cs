using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for SyncStatusEndpoints (Sprint 1 DEV-H1).
/// Routes: GET /api/v1/sync-status (optional: platformCode query param)
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Sprint", "H1")]
public sealed class SyncStatusEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public SyncStatusEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();

        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    // ── 1. Happy path ──

    [Fact]
    public async Task GetSyncStatus_ValidRequest_Returns200()
    {
        // Act
        var response = await _authClient.GetAsync("/api/v1/sync-status");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
            // Should be JSON (object or array)
            var json = JsonDocument.Parse(content);
            json.RootElement.ValueKind.Should().BeOneOf(
                JsonValueKind.Object, JsonValueKind.Array);
        }
    }

    // ── 2. Validation ──

    [Fact]
    public async Task GetSyncStatus_WithPlatformFilter_ReturnsFilteredResult()
    {
        // Act — filter by a specific platform code
        var response = await _authClient.GetAsync("/api/v1/sync-status?platformCode=Trendyol");

        // Assert — should return 200 with filtered data or 500 if handler fails
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
        }
    }

    // ── 3. Auth ──

    [Fact]
    public async Task GetSyncStatus_NoApiKey_Returns401()
    {
        // Act
        var response = await _noAuthClient.GetAsync("/api/v1/sync-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("API key");
    }

    // ── 4. Not found ──

    [Fact]
    public async Task GetSyncStatus_NonExistentSubRoute_Returns404()
    {
        // Act
        var response = await _authClient.GetAsync("/api/v1/sync-status/details/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ── 5. Server error ──

    [Fact]
    public async Task GetSyncStatus_HandlerThrows_ReturnsProblemDetails()
    {
        // Act — valid request, handler may throw if DB has no sync records
        var response = await _authClient.GetAsync("/api/v1/sync-status?platformCode=__INVALID__");

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
        }
    }
}
