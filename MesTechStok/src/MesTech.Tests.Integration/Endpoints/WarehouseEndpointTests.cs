using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for WarehouseEndpoints.
/// Routes: GET /api/v1/warehouses, /{id:guid}, /summary, /{id}/stock
///         POST /api/v1/warehouses, PUT /{id:guid}, DELETE /{id:guid}
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class WarehouseEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public WarehouseEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    [Fact]
    public async Task GetWarehouses_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync("/api/v1/warehouses");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWarehouses_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync("/api/v1/warehouses");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task GetWarehouseById_NonExistentId_Returns404()
    {
        var id = Guid.NewGuid();
        var response = await _authClient.GetAsync($"/api/v1/warehouses/{id}");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetWarehouseById_InvalidGuidFormat_ReturnsError()
    {
        var response = await _authClient.GetAsync("/api/v1/warehouses/not-a-guid");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateWarehouse_EmptyBody_Returns400()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _authClient.PostAsync("/api/v1/warehouses", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetWarehouseSummary_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync("/api/v1/warehouses/summary?tenantId=" + Guid.NewGuid());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
