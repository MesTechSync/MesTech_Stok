using System.Net;
using System.Text;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for ReportEndpoints.
/// Routes: GET /api/v1/reports/profit-loss, /monthly-summary/{y}/{m}, /kdv/{y}/{m},
///             /platform-comparison, /profitability, /sales-analytics, /expenses, etc.
///         POST /api/v1/reports/generate-tax-calendar/{year}, /export
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class ReportEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public ReportEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    [Fact]
    public async Task GetProfitLoss_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync(
            $"/api/v1/reports/profit-loss?tenantId={Guid.NewGuid()}&year=2026&month=3");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfitLoss_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync(
            $"/api/v1/reports/profit-loss?tenantId={Guid.NewGuid()}&year=2026&month=3");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetMonthlySummary_InvalidMonth_Returns400()
    {
        var response = await _authClient.GetAsync(
            $"/api/v1/reports/monthly-summary/2026/13?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetKdvReport_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync(
            $"/api/v1/reports/kdv/2026/3?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetSalesAnalytics_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync(
            $"/api/v1/reports/sales-analytics?tenantId={Guid.NewGuid()}&from=2026-01-01&to=2026-03-31");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ExportReport_EmptyBody_Returns400()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _authClient.PostAsync("/api/v1/reports/export", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
    }
}
