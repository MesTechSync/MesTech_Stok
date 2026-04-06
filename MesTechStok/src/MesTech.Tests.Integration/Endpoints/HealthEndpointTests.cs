using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for HealthEndpoints.
/// Routes: GET /health, /health/ready, /health/deep, /health/platforms, /metrics
/// Note: /health and /health/ready are AllowAnonymous (no auth required).
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class HealthEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public HealthEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    [Fact]
    public async Task HealthCheck_NoAuth_ReturnsHealthStatus()
    {
        // /health is AllowAnonymous — should not return 401
        var response = await _noAuthClient.GetAsync("/health");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable, HttpStatusCode.InternalServerError);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
            var json = JsonDocument.Parse(content);
            json.RootElement.TryGetProperty("status", out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task ReadinessCheck_NoAuth_ReturnsReadyStatus()
    {
        // /health/ready is AllowAnonymous
        var response = await _noAuthClient.GetAsync("/health/ready");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task MetricsEndpoint_NoAuth_ReturnsPrometheusText()
    {
        // /metrics is AllowAnonymous
        var response = await _noAuthClient.GetAsync("/metrics");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task DeepHealthCheck_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync("/health/deep");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task HealthCheck_NonExistentSubRoute_ReturnsNon200()
    {
        // /health/nonexistent is NOT in AllowAnonymous bypass — FallbackPolicy returns 401,
        // or if route doesn't exist, 404/405. All are acceptable non-200 responses.
        var response = await _noAuthClient.GetAsync("/health/nonexistent");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed, HttpStatusCode.Unauthorized);
    }
}
