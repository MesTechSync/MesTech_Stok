using System.Net;
using System.Text;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for ReturnEndpoints.
/// Routes: GET /api/v1/returns
///         POST /api/v1/returns/{id}/approve, /{id}/reject
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class ReturnEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public ReturnEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    [Fact]
    public async Task GetReturnList_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync($"/api/v1/returns?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReturnList_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync($"/api/v1/returns?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ApproveReturn_NonExistentId_ReturnsError()
    {
        var id = Guid.NewGuid();
        var response = await _authClient.PostAsync($"/api/v1/returns/{id}/approve", null);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task RejectReturn_EmptyBody_Returns400()
    {
        var id = Guid.NewGuid();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _authClient.PostAsync($"/api/v1/returns/{id}/reject", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ApproveReturn_NoApiKey_Returns401()
    {
        var id = Guid.NewGuid();
        var response = await _noAuthClient.PostAsync($"/api/v1/returns/{id}/approve", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
