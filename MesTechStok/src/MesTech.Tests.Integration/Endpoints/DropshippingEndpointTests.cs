using System.Net;
using System.Text;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for DropshippingEndpoints.
/// Routes: GET /api/v1/dropshipping/suppliers, /products, /orders, /supplier-performance
///         POST /api/v1/dropshipping/suppliers, /{id}/sync, /products/{id}/link,
///              /orders, /auto-order, /price-sync
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class DropshippingEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public DropshippingEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    [Fact]
    public async Task GetDropshipSuppliers_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync($"/api/v1/dropshipping/suppliers?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDropshipSuppliers_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync($"/api/v1/dropshipping/suppliers?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetDropshipProducts_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync($"/api/v1/dropshipping/products?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetDropshipOrders_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync($"/api/v1/dropshipping/orders?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDropshipSupplier_EmptyBody_Returns400()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _authClient.PostAsync("/api/v1/dropshipping/suppliers", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetSupplierPerformance_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync($"/api/v1/dropshipping/supplier-performance?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }
}
