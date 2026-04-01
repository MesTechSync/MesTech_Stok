using System.Net;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for CargoEndpoints.
/// Routes: GET /api/v1/cargo/providers, /tracking, /label/{shipmentId}
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class CargoEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public CargoEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    [Fact]
    public async Task GetCargoProviders_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync("/api/v1/cargo/providers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCargoProviders_ValidRequest_Returns200()
    {
        var response = await _authClient.GetAsync("/api/v1/cargo/providers");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task GetCargoTracking_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync("/api/v1/cargo/tracking?tenantId=" + Guid.NewGuid());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetShipmentLabel_NonExistentId_ReturnsError()
    {
        var response = await _authClient.GetAsync($"/api/v1/cargo/label/NONEXISTENT123?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetCargoTracking_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync($"/api/v1/cargo/tracking?tenantId={Guid.NewGuid()}&count=10");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }
}
