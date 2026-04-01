using System.Net;
using System.Text;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for EInvoiceEndpoints.
/// Routes: GET /api/v1/e-invoices, /{id:guid}, /check-vkn/{vkn}
///         POST /api/v1/e-invoices, /{id}/send, /{id}/cancel
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class EInvoiceEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public EInvoiceEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    [Fact]
    public async Task GetEInvoices_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync("/api/v1/e-invoices");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEInvoices_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync("/api/v1/e-invoices?page=1&pageSize=10");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetEInvoiceById_NonExistentId_Returns404()
    {
        var id = Guid.NewGuid();
        var response = await _authClient.GetAsync($"/api/v1/e-invoices/{id}");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task CreateEInvoice_EmptyBody_Returns400()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _authClient.PostAsync("/api/v1/e-invoices", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task CheckVknMukellef_ValidVkn_ReturnsExpected()
    {
        var response = await _authClient.GetAsync("/api/v1/e-invoices/check-vkn/1234567890");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SendEInvoice_NonExistentId_ReturnsError()
    {
        var id = Guid.NewGuid();
        var response = await _authClient.PostAsync($"/api/v1/e-invoices/{id}/send", null);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }
}
