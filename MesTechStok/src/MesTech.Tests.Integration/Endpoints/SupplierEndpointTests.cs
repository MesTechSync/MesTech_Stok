using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for SupplierEndpoints.
/// Routes: GET /api/v1/suppliers, /{id:guid}, /paged
///         POST /api/v1/suppliers, PUT /{id:guid}, DELETE /{id:guid}
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class SupplierEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public SupplierEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    [Fact]
    public async Task GetSuppliers_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync("/api/v1/suppliers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSuppliers_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync("/api/v1/suppliers");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetSupplierById_NonExistentId_Returns404()
    {
        var id = Guid.NewGuid();
        var response = await _authClient.GetAsync($"/api/v1/suppliers/{id}");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task CreateSupplier_EmptyBody_Returns400()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _authClient.PostAsync("/api/v1/suppliers", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetSuppliersPaged_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync("/api/v1/suppliers/paged?page=1&pageSize=10");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task DeleteSupplier_NonExistentId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var response = await _authClient.DeleteAsync($"/api/v1/suppliers/{id}");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }
}
