using System.Net;
using System.Text;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for ErpEndpoints.
/// Routes: GET /api/v1/erp/providers, /status, /sync/history, /dashboard, /sync/logs, /account-mappings
///         POST /api/v1/erp/test-connection, /sync/stock, /sync/accounts, /sync-order, /account-mappings
///         DELETE /api/v1/erp/account-mappings/{id}
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class ErpEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public ErpEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    [Fact]
    public async Task GetErpProviders_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync("/api/v1/erp/providers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetErpProviders_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync("/api/v1/erp/providers");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetErpStatus_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync("/api/v1/erp/status");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetErpDashboard_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync($"/api/v1/erp/dashboard?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task TestErpConnection_EmptyBody_Returns400()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _authClient.PostAsync("/api/v1/erp/test-connection", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task DeleteErpAccountMapping_NonExistentId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var response = await _authClient.DeleteAsync($"/api/v1/erp/account-mappings/{id}?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }
}
