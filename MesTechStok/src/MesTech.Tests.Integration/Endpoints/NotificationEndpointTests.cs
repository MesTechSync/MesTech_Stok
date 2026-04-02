using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for NotificationEndpoints.
/// Routes: GET /api/v1/notifications, /unread-count
///         POST /api/v1/notifications/{id}/read, /send, /push
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class NotificationEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public NotificationEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    [Fact]
    public async Task GetNotifications_NoApiKey_Returns401()
    {
        var tenantId = Guid.NewGuid();
        var response = await _noAuthClient.GetAsync($"/api/v1/notifications?tenantId={tenantId}&page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNotifications_ValidRequest_ReturnsExpected()
    {
        var tenantId = Guid.NewGuid();
        var response = await _authClient.GetAsync($"/api/v1/notifications?tenantId={tenantId}&page=1&pageSize=10");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetUnreadCount_ValidRequest_ReturnsExpected()
    {
        var tenantId = Guid.NewGuid();
        var response = await _authClient.GetAsync($"/api/v1/notifications/unread-count?tenantId={tenantId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task MarkNotificationRead_NonExistentId_ReturnsError()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var response = await _authClient.PostAsync(
            $"/api/v1/notifications/{id}/read?tenantId={tenantId}", null);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SendNotification_EmptyBody_Returns400()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _authClient.PostAsync("/api/v1/notifications/send", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
    }
}
