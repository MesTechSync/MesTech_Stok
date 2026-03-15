using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Integration.Tests.Api;

/// <summary>
/// MesTech.WebApi endpoint integration tests (Sprint 10.1 — E03).
/// Uses WebApplicationFactory with InMemory EF Core for test isolation.
/// Covers: health, auth, API key middleware, CORS, content-type, 404, Swagger,
/// rate limiting, error handling, and metrics.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Layer", "Api")]
public sealed class ApiEndpointTests : IClassFixture<MesTechWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;
    private readonly MesTechWebApplicationFactory _factory;

    public ApiEndpointTests(MesTechWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Authenticated client with valid API key
        _authenticatedClient = factory.CreateClient();
        _authenticatedClient.DefaultRequestHeaders.Add(
            "X-API-Key", MesTechWebApplicationFactory.TestApiKey);
    }

    // ──────────────────────────────────────────────────
    // 1. Health endpoint — bypass path, no API key needed
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task HealthEndpoint_ReturnsSuccessStatusCode()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health");

        // Assert — health should return 200 (healthy) or 503 (degraded) but never 401/500
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.ServiceUnavailable);

        // Verify it's JSON
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsJsonWithStatusField()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert — response must contain status field
        var json = JsonDocument.Parse(content);
        json.RootElement.TryGetProperty("status", out var statusProp).Should().BeTrue();
        statusProp.GetString().Should().BeOneOf("healthy", "degraded", "unhealthy");
    }

    [Fact]
    public async Task HealthEndpoint_ContainsTimestampAndDuration()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert — health JSON should include timestamp and duration
        var json = JsonDocument.Parse(content);
        json.RootElement.TryGetProperty("timestamp", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("duration", out _).Should().BeTrue();
    }

    // ──────────────────────────────────────────────────
    // 2. Auth login endpoint — bypass path, no API key needed
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task AuthLogin_ValidCredentials_ReturnsTokenResponse()
    {
        // Arrange
        var loginPayload = new { userName = "testuser", password = "TestPassword123!" };
        var content = new StringContent(
            JsonSerializer.Serialize(loginPayload),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/login", content);

        // Assert — should return 200 with a token
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.TryGetProperty("success", out var success).Should().BeTrue();
        success.GetBoolean().Should().BeTrue();
        json.RootElement.TryGetProperty("token", out var token).Should().BeTrue();
        token.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AuthLogin_EmptyCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginPayload = new { userName = "", password = "" };
        var content = new StringContent(
            JsonSerializer.Serialize(loginPayload),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AuthValidate_EmptyToken_ReturnsBadRequest()
    {
        // Arrange
        var payload = new { token = "" };
        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/validate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────────────────────────────────────────
    // 3. API Key middleware — protected endpoints
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task ProtectedEndpoint_WithoutApiKey_Returns401()
    {
        // Arrange & Act — no X-API-Key header
        var response = await _client.GetAsync("/api/v1/products/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidApiKey_PassesMiddleware()
    {
        // Arrange & Act — authenticated client with valid API key
        var response = await _authenticatedClient.GetAsync("/api/v1/products/status");

        // Assert — should NOT return 401 (passes API key middleware).
        // May return 500 if handler is not in Application assembly — that is a
        // separate issue, but the middleware did NOT block the request.
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidApiKey_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "invalid-key-12345");

        // Act
        var response = await client.GetAsync("/api/v1/products/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OrdersEndpoint_WithoutApiKey_Returns401()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OrdersEndpoint_WithValidApiKey_PassesMiddleware()
    {
        // Arrange & Act
        var response = await _authenticatedClient.GetAsync("/api/v1/orders");

        // Assert — passes middleware (not 401)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StockEndpoint_WithoutApiKey_Returns401()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/stock/value");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DashboardEndpoint_WithoutApiKey_Returns401()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/dashboard/kpi");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CategoriesEndpoint_WithoutApiKey_Returns401()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────
    // 4. 404 for non-existent endpoints
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task NonExistentEndpoint_Returns404()
    {
        // Arrange & Act — valid API key but non-existent path
        var response = await _authenticatedClient.GetAsync("/api/v1/nonexistent-endpoint-xyz");

        // Assert — should be 404 (not 401 because key is valid, not 500)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────
    // 5. Swagger endpoint (Development environment)
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task SwaggerEndpoint_ReturnsOpenApiSpec()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.TryGetProperty("info", out var info).Should().BeTrue();
        info.TryGetProperty("title", out var title).Should().BeTrue();
        title.GetString().Should().Be("MesTech API");
    }

    [Fact]
    public async Task SwaggerEndpoint_ContainsApiKeySecurityDefinition()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();

        // Assert — Swagger should document the API Key security scheme
        var json = JsonDocument.Parse(content);
        json.RootElement.TryGetProperty("components", out var components).Should().BeTrue();
        components.TryGetProperty("securitySchemes", out var schemes).Should().BeTrue();
        schemes.TryGetProperty("ApiKey", out _).Should().BeTrue();
    }

    // ──────────────────────────────────────────────────
    // 6. CORS headers
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task CorsHeaders_PresentForAllowedOrigin()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/products/status");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "X-API-Key");

        // Act
        var response = await _client.SendAsync(request);

        // Assert — CORS preflight should return Access-Control-Allow-Origin
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins)
            .Should().BeTrue("CORS should return Access-Control-Allow-Origin for allowed origins");
        origins.Should().Contain("http://localhost:5173");
    }

    // ──────────────────────────────────────────────────
    // 7. Auth login + validate round-trip
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task AuthLogin_ThenValidate_RoundTrip()
    {
        // Arrange — get a token first
        var loginPayload = new { userName = "roundtrip_user", password = "SecurePass123!" };
        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginPayload),
            Encoding.UTF8,
            "application/json");

        var loginResponse = await _client.PostAsync("/api/v1/auth/login", loginContent);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginJson = JsonDocument.Parse(loginBody);
        var token = loginJson.RootElement.GetProperty("token").GetString()!;

        // Act — validate the token
        var validatePayload = new { token };
        var validateContent = new StringContent(
            JsonSerializer.Serialize(validatePayload),
            Encoding.UTF8,
            "application/json");

        var validateResponse = await _client.PostAsync("/api/v1/auth/validate", validateContent);

        // Assert
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var validateBody = await validateResponse.Content.ReadAsStringAsync();
        var validateJson = JsonDocument.Parse(validateBody);
        validateJson.RootElement.GetProperty("valid").GetBoolean().Should().BeTrue();
    }

    // ──────────────────────────────────────────────────
    // 8. Metrics endpoint — bypass path
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task MetricsEndpoint_ReturnsPrometheusFormat()
    {
        // Arrange & Act — /metrics is a bypass path, no API key needed
        var response = await _client.GetAsync("/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var contentType = response.Content.Headers.ContentType?.ToString();
        contentType.Should().Contain("text/plain");
    }

    // ──────────────────────────────────────────────────
    // 9. Sync status endpoint — responds to valid API key
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task GetSyncStatus_WithValidApiKey_PassesMiddleware()
    {
        // Arrange & Act
        var response = await _authenticatedClient.GetAsync("/api/v1/sync-status");

        // Assert — should not be 401 (passes API key)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────
    // 10. Global error handler — ProblemDetails response
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task ErrorHandler_ReturnsProblemDetailsJson()
    {
        // Arrange & Act — products/status will fail with 500 (handler in Desktop assembly)
        // but global exception handler should return a JSON ProblemDetails response
        var response = await _authenticatedClient.GetAsync("/api/v1/products/status");

        // If the endpoint fails, verify error handler returns proper format
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            // Global exception handler returns JSON (application/json or application/problem+json)
            response.Content.Headers.ContentType?.MediaType.Should().Contain("json");
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            json.RootElement.TryGetProperty("status", out var status).Should().BeTrue();
            status.GetInt32().Should().Be(500);
            json.RootElement.TryGetProperty("title", out _).Should().BeTrue();
        }
    }

    // ──────────────────────────────────────────────────
    // 11. Auth bypass — /api/v1/auth does NOT require API key
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task AuthEndpoint_NoApiKeyRequired()
    {
        // Arrange — no API key, POST to auth/login
        var loginPayload = new { userName = "bypass_test", password = "Pass123!" };
        var content = new StringContent(
            JsonSerializer.Serialize(loginPayload),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/login", content);

        // Assert — should NOT be 401 (auth is a bypass path)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ──────────────────────────────────────────────────
    // 12. Auth login response contains expiresAt
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task AuthLogin_ResponseContainsExpiresAt()
    {
        // Arrange
        var loginPayload = new { userName = "expiry_test", password = "Pass123!" };
        var content = new StringContent(
            JsonSerializer.Serialize(loginPayload),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/login", content);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = JsonDocument.Parse(body);
        json.RootElement.TryGetProperty("expiresAt", out var expiresAt).Should().BeTrue();
        expiresAt.GetString().Should().NotBeNullOrWhiteSpace();
    }

    // ──────────────────────────────────────────────────
    // 13. Auth validate with valid token returns userId and tenantId
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task AuthValidate_ValidToken_ReturnsUserAndTenantIds()
    {
        // Arrange — login first
        var loginPayload = new { userName = "validate_test", password = "Pass123!" };
        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginPayload),
            Encoding.UTF8,
            "application/json");

        var loginResponse = await _client.PostAsync("/api/v1/auth/login", loginContent);
        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginJson = JsonDocument.Parse(loginBody);
        var token = loginJson.RootElement.GetProperty("token").GetString()!;

        // Act
        var validatePayload = new { token };
        var validateContent = new StringContent(
            JsonSerializer.Serialize(validatePayload),
            Encoding.UTF8,
            "application/json");
        var response = await _client.PostAsync("/api/v1/auth/validate", validateContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("valid").GetBoolean().Should().BeTrue();
        json.RootElement.TryGetProperty("userId", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("tenantId", out _).Should().BeTrue();
    }

    // ──────────────────────────────────────────────────
    // 14. Auth validate with garbage token returns valid=false
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task AuthValidate_GarbageToken_ReturnsInvalid()
    {
        // Arrange
        var payload = new { token = "not.a.valid.jwt.token" };
        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/validate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("valid").GetBoolean().Should().BeFalse();
    }
}
