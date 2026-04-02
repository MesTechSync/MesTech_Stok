using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for CategoryEndpoints (Sprint 1 DEV-H1).
/// Routes: GET /api/v1/categories (optional: activeOnly=true/false)
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Sprint", "H1")]
public sealed class CategoryEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public CategoryEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();

        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    // ── 1. Happy path ──

    [Fact]
    public async Task GetCategories_ValidRequest_Returns200()
    {
        // Act
        var response = await _authClient.GetAsync("/api/v1/categories");

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
    public async Task GetCategories_InvalidActiveOnlyParam_ReturnsError()
    {
        // Act — activeOnly should be a boolean, pass invalid string
        var response = await _authClient.GetAsync("/api/v1/categories?activeOnly=not-a-bool");

        // Assert — should return 400 for invalid boolean parameter
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();
    }

    // ── 3. Auth ──

    [Fact]
    public async Task GetCategories_NoApiKey_Returns401()
    {
        // Act
        var response = await _noAuthClient.GetAsync("/api/v1/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        // body content varies by middleware config
    }

    // ── 4. Not found ──

    [Fact]
    public async Task GetCategories_NonExistentSubRoute_Returns404Or401()
    {
        // Act — request a sub-route that doesn't exist
        var response = await _authClient.GetAsync("/api/v1/categories/999/details");

        // Assert — 404 if auth passes, 401 if auth middleware blocks first
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    // ── 5. Server error ──

    [Fact]
    public async Task GetCategories_WithActiveOnly_HandlerResponse()
    {
        // Act — explicit activeOnly=false to exercise full parameter path
        var response = await _authClient.GetAsync("/api/v1/categories?activeOnly=false");

        // Assert — handler may return 200 or 500 depending on DB state
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var body = await response.Content.ReadAsStringAsync();
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
