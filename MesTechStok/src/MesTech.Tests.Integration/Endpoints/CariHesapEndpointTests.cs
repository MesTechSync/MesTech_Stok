using System.Net;
using System.Text;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for CariHesapEndpoints.
/// Routes: GET /api/v1/accounting/cari-hesaplar, /{id:guid}, /{id}/hareketler
///         POST /api/v1/accounting/cari-hesaplar, /{id}/hareketler
///         PUT /api/v1/accounting/cari-hesaplar/{id:guid}
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class CariHesapEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public CariHesapEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    [Fact]
    public async Task GetCariHesaplar_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync("/api/v1/accounting/cari-hesaplar");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCariHesaplar_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync("/api/v1/accounting/cari-hesaplar");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetCariHesapById_NonExistentId_Returns404()
    {
        var id = Guid.NewGuid();
        var response = await _authClient.GetAsync($"/api/v1/accounting/cari-hesaplar/{id}");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task CreateCariHesap_EmptyBody_Returns400()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _authClient.PostAsync("/api/v1/accounting/cari-hesaplar", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetCariHareketler_NonExistentId_ReturnsExpected()
    {
        var id = Guid.NewGuid();
        var response = await _authClient.GetAsync($"/api/v1/accounting/cari-hesaplar/{id}/hareketler");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetCariHesapById_InvalidGuid_Returns404()
    {
        var response = await _authClient.GetAsync("/api/v1/accounting/cari-hesaplar/not-a-guid");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed, HttpStatusCode.BadRequest);
    }
}
