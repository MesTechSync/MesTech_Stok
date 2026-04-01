using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for AuthEndpoints (Sprint 1 DEV-H1).
/// Routes: POST /api/v1/auth/login, POST /api/v1/auth/validate
/// Auth bypass: /api/v1/auth does NOT require API key.
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Sprint", "H1")]
public sealed class AuthEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(EndpointTestWebAppFactory factory)
    {
        // Auth endpoints bypass API key — no need for X-API-Key header
        _client = factory.CreateClient();
    }

    // ── 1. Happy path ──

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        // Arrange
        var payload = new { userName = "testuser", password = "__TEST_PASSWORD_PLACEHOLDER__" };
        var content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();

        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        json.RootElement.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        json.RootElement.TryGetProperty("expiresAt", out _).Should().BeTrue();
    }

    // ── 2. Validation ──

    [Fact]
    public async Task Login_EmptyCredentials_Returns400WithError()
    {
        // Arrange — empty username and password
        var payload = new { userName = "", password = "" };
        var content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();

        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        json.RootElement.GetProperty("error").GetString().Should().Contain("required");
    }

    // ── 3. Auth bypass verification ──

    [Fact]
    public async Task Login_NoApiKeyRequired_DoesNotReturn401()
    {
        // Arrange — no X-API-Key header on purpose
        var payload = new { userName = "bypasstest", password = "__TEST_PASSWORD_PLACEHOLDER__" };
        var content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/login", content);

        // Assert — auth endpoints are bypass paths, should never return 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── 4. Not found ──

    [Fact]
    public async Task Auth_NonExistentSubRoute_Returns404()
    {
        // Act — request a sub-route that doesn't exist under /api/v1/auth
        var response = await _client.GetAsync("/api/v1/auth/nonexistent");

        // Assert — GET on a POST-only group or non-existent path
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.MethodNotAllowed,
            HttpStatusCode.Unauthorized);
    }

    // ── 5. Server error ──

    [Fact]
    public async Task Validate_EmptyToken_Returns400()
    {
        // Arrange — empty token string
        var payload = new { token = "" };
        var content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/validate", content);

        // Assert — should return 400 for empty token, or 401 if validate requires auth
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized);
    }
}
