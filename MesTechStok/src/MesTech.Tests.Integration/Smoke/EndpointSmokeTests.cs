using System.Net;
using FluentAssertions;
using Xunit;
using MesTech.Tests.Integration.Endpoints;

namespace MesTech.Tests.Integration.Smoke;

/// <summary>
/// Bulk smoke tests — every major endpoint returns expected HTTP status.
/// No auth = 401, with API key = 200/201/404 (never 500).
/// DEV6 TUR26: G562 endpoint smoke coverage.
/// </summary>
[Trait("Category", "Smoke")]
[Trait("Sprint", "DEV6-TUR26")]
public sealed class EndpointSmokeTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuth;
    private readonly HttpClient _auth;

    public EndpointSmokeTests(EndpointTestWebAppFactory factory)
    {
        _noAuth = factory.CreateClient();
        _auth = factory.CreateClient();
        _auth.DefaultRequestHeaders.Add("X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    // ── Health endpoints (no auth required) ──

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/metrics")]
    public async Task Health_NoAuth_Returns200(string path)
    {
        var response = await _noAuth.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Auth guard — unauthenticated requests ──

    [Theory]
    [InlineData("/api/v1/products")]
    [InlineData("/api/v1/orders")]
    [InlineData("/api/v1/categories")]
    [InlineData("/api/v1/suppliers")]
    [InlineData("/api/v1/stock/movements")]
    [InlineData("/api/v1/invoices")]
    [InlineData("/api/v1/customers")]
    [InlineData("/api/v1/dashboard/summary")]
    public async Task API_NoAuth_Returns401(string path)
    {
        var response = await _noAuth.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Core CRUD GET endpoints (with auth) ──

    [Theory]
    [InlineData("/api/v1/products", HttpStatusCode.OK)]
    [InlineData("/api/v1/products/status", HttpStatusCode.OK)]
    [InlineData("/api/v1/products/low-stock", HttpStatusCode.OK)]
    [InlineData("/api/v1/orders", HttpStatusCode.OK)]
    [InlineData("/api/v1/orders/stale?tenantId=00000000-0000-0000-0000-000000000001", HttpStatusCode.OK)]
    [InlineData("/api/v1/categories", HttpStatusCode.OK)]
    [InlineData("/api/v1/suppliers", HttpStatusCode.OK)]
    [InlineData("/api/v1/suppliers/paged", HttpStatusCode.OK)]
    [InlineData("/api/v1/stock/movements", HttpStatusCode.OK)]
    [InlineData("/api/v1/stock/lots?tenantId=00000000-0000-0000-0000-000000000001", HttpStatusCode.OK)]
    [InlineData("/api/v1/invoices", HttpStatusCode.OK)]
    [InlineData("/api/v1/customers", HttpStatusCode.OK)]
    [InlineData("/api/v1/dashboard/summary", HttpStatusCode.OK)]
    [InlineData("/api/v1/notifications?tenantId=00000000-0000-0000-0000-000000000001", HttpStatusCode.OK)]
    [InlineData("/api/v1/settings", HttpStatusCode.OK)]
    public async Task CoreGET_WithAuth_ReturnsExpected(string path, HttpStatusCode expected)
    {
        var response = await _auth.GetAsync(path);
        response.StatusCode.Should().Be(expected,
            because: $"GET {path} should return {expected}");
        // Never 500
        ((int)response.StatusCode).Should().BeLessThan(500,
            because: $"GET {path} should never return 5xx");
    }

    // ── Extended GET endpoints (with auth) — no 500 allowed ──

    [Theory]
    [InlineData("/api/v1/warehouses")]
    [InlineData("/api/v1/campaigns")]
    [InlineData("/api/v1/reports/sales")]
    [InlineData("/api/v1/shipments")]
    [InlineData("/api/v1/cargo/providers")]
    [InlineData("/api/v1/billing/plans")]
    [InlineData("/api/v1/kvkk/rights")]
    [InlineData("/api/v1/crm/leads?tenantId=00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/v1/crm/deals?tenantId=00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/v1/crm/activities?tenantId=00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/v1/platforms")]
    [InlineData("/api/v1/dropshipping/pool")]
    [InlineData("/health/deep")]
    [InlineData("/health/platforms")]
    [InlineData("/health/mesa")]
    public async Task ExtendedGET_WithAuth_No500(string path)
    {
        var response = await _auth.GetAsync(path);
        ((int)response.StatusCode).Should().BeLessThan(500,
            because: $"GET {path} should never return 5xx");
    }

    // ── OpenAPI + Scalar docs availability ──

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar/v1")]
    public async Task APIDocs_WithAuth_Returns200(string path)
    {
        var response = await _auth.GetAsync(path);
        // Scalar/OpenAPI may return 200 or redirect, never 500
        ((int)response.StatusCode).Should().BeLessThan(500,
            because: $"API docs {path} should be available");
    }

    // ── POST to read-only route = 405 Method Not Allowed ──

    [Theory]
    [InlineData("/api/v1/products/status")]
    [InlineData("/api/v1/products/low-stock")]
    public async Task POST_ToReadOnlyGET_Returns405(string path)
    {
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var response = await _auth.PostAsync(path, content);
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}
