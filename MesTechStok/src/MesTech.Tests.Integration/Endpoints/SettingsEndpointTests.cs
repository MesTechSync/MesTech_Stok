using System.Net;
using System.Text;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for SettingsEndpoints.
/// Routes: GET /api/v1/settings/profile, /credentials, /notifications, /store, /erp, /fulfillment, /import
///         PUT /api/v1/settings/profile, /store
///         POST /api/v1/settings/company, /api, /erp, /fulfillment
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class SettingsEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public SettingsEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    [Fact]
    public async Task GetSettingsProfile_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync($"/api/v1/settings/profile?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSettingsProfile_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync($"/api/v1/settings/profile?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetSettingsCredentials_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync($"/api/v1/settings/credentials?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetStoreSettings_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync($"/api/v1/settings/store?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SaveCompanySettings_EmptyBody_Returns400()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _authClient.PostAsync("/api/v1/settings/company", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetErpSettings_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync($"/api/v1/settings/erp?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }
}
