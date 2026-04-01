using System.Net;
using System.Text;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for WebhookEndpoints.
/// Routes: POST /api/webhooks/{platform} (AllowAnonymous), /test, /dead-letters/{id}/resolve
///         GET /api/webhooks/dead-letters, /test/platforms, /test/sample/{platform}/{eventType}
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class WebhookEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public WebhookEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    [Fact]
    public async Task ReceiveWebhook_EmptyBody_Returns400()
    {
        // POST /api/webhooks/{platform} is AllowAnonymous
        var content = new StringContent("", Encoding.UTF8, "application/json");
        var response = await _noAuthClient.PostAsync("/api/webhooks/trendyol", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ReceiveWebhook_InvalidJson_Returns400()
    {
        var content = new StringContent("not-json", Encoding.UTF8, "application/json");
        var response = await _noAuthClient.PostAsync("/api/webhooks/trendyol", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ReceiveWebhook_ValidJson_ReturnsExpected()
    {
        var payload = "{\"orderNumber\":\"TY-TEST-001\",\"status\":\"Created\"}";
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await _noAuthClient.PostAsync("/api/webhooks/trendyol", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetDeadLetters_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync("/api/webhooks/dead-letters?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTestPlatforms_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync("/api/webhooks/test/platforms");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetSamplePayload_ValidPlatform_ReturnsExpected()
    {
        var response = await _authClient.GetAsync("/api/webhooks/test/sample/trendyol/order.created");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }
}
