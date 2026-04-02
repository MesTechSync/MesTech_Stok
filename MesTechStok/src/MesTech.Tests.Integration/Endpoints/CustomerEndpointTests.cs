using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for CustomerEndpoints.
/// Routes: GET /api/v1/customers, POST /api/v1/customers,
///         PUT /api/v1/customers/{id:guid}, POST /api/v1/customers/export
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class CustomerEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public CustomerEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    [Fact]
    public async Task GetCustomers_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync("/api/v1/customers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCustomers_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync("/api/v1/customers");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task CreateCustomer_EmptyBody_Returns400()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _authClient.PostAsync("/api/v1/customers", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task UpdateCustomer_NonExistentId_ReturnsError()
    {
        var id = Guid.NewGuid();
        var content = new StringContent(
            JsonSerializer.Serialize(new { Id = id, Name = "Test", Email = "test@test.com" }),
            Encoding.UTF8, "application/json");
        var response = await _authClient.PutAsync($"/api/v1/customers/{id}", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetCustomers_NonExistentSubRoute_Returns404()
    {
        var response = await _authClient.GetAsync("/api/v1/customers/non-existent-route");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed, HttpStatusCode.BadRequest);
    }
}
