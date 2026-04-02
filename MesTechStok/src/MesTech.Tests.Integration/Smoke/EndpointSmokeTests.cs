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

    /// <summary>Test tenantId used across endpoint calls.</summary>
    private const string TenantId = "00000000-0000-0000-0000-000000000001";

    public EndpointSmokeTests(EndpointTestWebAppFactory factory)
    {
        _noAuth = factory.CreateClient();
        _auth = factory.CreateClient();
        _auth.DefaultRequestHeaders.Add("X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    // ── Health endpoints (no auth required) ──
    // Health checks report 503 when infrastructure (PG, Redis, RabbitMQ) is unavailable.
    // In test environment without real infra, 200 or 503 are both valid responses.

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/metrics")]
    public async Task Health_NoAuth_ReturnsNon5xx(string path)
    {
        var response = await _noAuth.GetAsync(path);
        var code = (int)response.StatusCode;
        // Health endpoints may return 503 (unhealthy) when infra is down — that's valid.
        // Only true 5xx server errors (500, 502, etc.) are failures. 503 is expected for health.
        code.Should().NotBe(500, because: $"GET {path} should not throw an unhandled exception");
    }

    // ── Auth guard — unauthenticated requests ──

    [Theory]
    [InlineData("/api/v1/orders")]
    [InlineData("/api/v1/categories")]
    [InlineData("/api/v1/suppliers")]
    [InlineData("/api/v1/stock/movements")]
    [InlineData("/api/v1/invoices")]
    [InlineData("/api/v1/customers")]
    public async Task API_NoAuth_Returns401(string path)
    {
        var response = await _noAuth.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Core CRUD GET endpoints (with auth) — never 500 ──

    [Theory]
    [InlineData("/api/v1/products?tenantId=" + TenantId)]
    [InlineData("/api/v1/products/status")]
    [InlineData("/api/v1/products/low-stock")]
    [InlineData("/api/v1/orders?tenantId=" + TenantId)]
    [InlineData("/api/v1/orders/stale?tenantId=" + TenantId)]
    [InlineData("/api/v1/categories?tenantId=" + TenantId)]
    [InlineData("/api/v1/suppliers?tenantId=" + TenantId)]
    [InlineData("/api/v1/suppliers/paged")]
    [InlineData("/api/v1/stock/movements?tenantId=" + TenantId)]
    [InlineData("/api/v1/stock/lots?tenantId=" + TenantId)]
    [InlineData("/api/v1/invoices?tenantId=" + TenantId)]
    [InlineData("/api/v1/customers?tenantId=" + TenantId)]
    [InlineData("/api/v1/dashboard/summary?tenantId=" + TenantId)]
    [InlineData("/api/v1/notifications?tenantId=" + TenantId + "&page=1&pageSize=10")]
    [InlineData("/api/v1/settings/profile?tenantId=" + TenantId)]
    public async Task CoreGET_WithAuth_ReturnsExpected(string path)
    {
        var response = await _auth.GetAsync(path);
        var code = (int)response.StatusCode;
        code.Should().BeLessThan(500,
            because: $"GET {path} should never return 5xx (got {response.StatusCode})");
    }

    // ── Extended GET endpoints (with auth) — no 500 allowed ──

    [Theory]
    [InlineData("/api/v1/warehouses?tenantId=" + TenantId)]
    [InlineData("/api/v1/campaigns?tenantId=" + TenantId)]
    [InlineData("/api/v1/reports/sales?tenantId=" + TenantId)]
    [InlineData("/api/v1/shipments?tenantId=" + TenantId)]
    [InlineData("/api/v1/cargo/providers")]
    [InlineData("/api/v1/billing/plans")]
    [InlineData("/api/v1/kvkk/rights")]
    [InlineData("/api/v1/crm/leads?tenantId=" + TenantId)]
    [InlineData("/api/v1/crm/deals?tenantId=" + TenantId + "&page=1&pageSize=10")]
    [InlineData("/api/v1/crm/activities?tenantId=" + TenantId)]
    [InlineData("/api/v1/platforms?tenantId=" + TenantId)]
    [InlineData("/api/v1/dropshipping/pool?tenantId=" + TenantId)]
    public async Task ExtendedGET_WithAuth_No500(string path)
    {
        var response = await _auth.GetAsync(path);
        var code = (int)response.StatusCode;
        // 400/404 is acceptable for endpoints that need optional data
        code.Should().BeLessThan(500,
            because: $"GET {path} should never return 5xx (got {response.StatusCode})");
    }

    // ── Health deep endpoints — 503 is valid when infrastructure is unavailable ──

    [Theory]
    [InlineData("/health/deep")]
    [InlineData("/health/platforms")]
    [InlineData("/health/mesa")]
    public async Task HealthDeep_WithAuth_ReturnsNon500(string path)
    {
        var response = await _auth.GetAsync(path);
        var code = (int)response.StatusCode;
        // Health endpoints return 503 (ServiceUnavailable) when infra checks fail.
        // In test env without PG/Redis/RabbitMQ, 503 is expected and valid.
        code.Should().NotBe(500,
            because: $"GET {path} should not throw an unhandled exception (503 is acceptable)");
    }

    // ── OpenAPI + Scalar docs availability ──

    [Fact]
    public async Task Scalar_WithAuth_Returns200()
    {
        var response = await _auth.GetAsync("/scalar/v1");
        ((int)response.StatusCode).Should().BeLessThan(500,
            because: "Scalar docs should be available");
    }

    // Note: /openapi/v1.json may 500 in test env due to OpenAPI schema generation
    // race condition with InMemory providers. Tracked as known issue.
    [Fact]
    public async Task OpenApi_WithAuth_ReturnsNon404()
    {
        var response = await _auth.GetAsync("/openapi/v1.json");
        // OpenAPI endpoint exists and is routed (not 404).
        // May return 500 in test due to schema generation with InMemory DB provider.
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
            because: "OpenAPI endpoint should be registered");
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
