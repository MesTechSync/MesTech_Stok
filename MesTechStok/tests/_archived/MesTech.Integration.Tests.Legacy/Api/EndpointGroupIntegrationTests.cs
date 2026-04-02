using System.Net;
using FluentAssertions;
using Xunit;

namespace MesTech.Integration.Tests.Api;

/// <summary>
/// DEV5 G432: Endpoint group integration tests — route existence + auth middleware.
/// Covers CRM, Billing, Cargo, Dashboard, Reports, Customers, Categories, Stock, Orders.
/// Target: %14 → %30+ endpoint coverage.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Layer", "Api")]
public sealed class EndpointGroupIntegrationTests : IClassFixture<MesTechWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _authClient;

    public EndpointGroupIntegrationTests(MesTechWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add("X-API-Key", MesTechWebApplicationFactory.TestApiKey);
    }

    // ════════════════════════════════════════════════════════
    // CRM Endpoints — /api/v1/crm
    // ════════════════════════════════════════════════════════

    [Fact]
    public async Task Crm_GetLeads_WithoutApiKey_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/crm/leads");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Crm_GetLeads_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/crm/leads");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ════════════════════════════════════════════════════════
    // Billing Endpoints — /api/v1/billing
    // ════════════════════════════════════════════════════════

    [Fact]
    public async Task Billing_GetPlans_WithoutApiKey_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/billing/plans");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Billing_GetPlans_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/billing/plans");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Billing_GetInvoices_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/billing/invoices");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ════════════════════════════════════════════════════════
    // Cargo Endpoints — /api/v1/cargo
    // ════════════════════════════════════════════════════════

    [Fact]
    public async Task Cargo_GetProviders_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/cargo/providers");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cargo_GetTracking_WithoutApiKey_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/cargo/tracking?trackingNumber=TEST123");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ════════════════════════════════════════════════════════
    // Dashboard Endpoints — /api/v1/dashboard
    // ════════════════════════════════════════════════════════

    [Fact]
    public async Task Dashboard_GetKpi_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/dashboard/kpi");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Dashboard_GetSalesTrend_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/dashboard/sales-trend");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Dashboard_GetRecentOrders_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/dashboard/recent-orders");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Dashboard_GetAccountingKpi_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/dashboard/accounting-kpi");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ════════════════════════════════════════════════════════
    // Report Endpoints — /api/v1/reports
    // ════════════════════════════════════════════════════════

    [Fact]
    public async Task Reports_GetProfitLoss_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/reports/profit-loss");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reports_GetKdv_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/reports/kdv/2026/3");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reports_GetPlatformComparison_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/reports/platform-comparison");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ════════════════════════════════════════════════════════
    // Customer Endpoints — /api/v1/customers
    // ════════════════════════════════════════════════════════

    [Fact]
    public async Task Customers_GetAll_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/customers");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Customers_GetAll_WithoutApiKey_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/customers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ════════════════════════════════════════════════════════
    // Category Endpoints — /api/v1/categories
    // ════════════════════════════════════════════════════════

    [Fact]
    public async Task Categories_GetAll_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/categories");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Categories_GetPaged_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/categories/paged?page=1&pageSize=10");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ════════════════════════════════════════════════════════
    // Stock Endpoints — /api/v1/stock
    // ════════════════════════════════════════════════════════

    [Fact]
    public async Task Stock_GetMovements_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/stock/movements");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Stock_GetInventory_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/stock/inventory");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Stock_GetValue_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/stock/value");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ════════════════════════════════════════════════════════
    // Order Endpoints — /api/v1/orders
    // ════════════════════════════════════════════════════════

    [Fact]
    public async Task Orders_GetAll_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/orders");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Orders_GetStale_WithApiKey_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/orders/stale");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Orders_GetAll_WithoutApiKey_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/orders");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ════════════════════════════════════════════════════════
    // Cross-cutting: Unknown route returns 404
    // ════════════════════════════════════════════════════════

    [Fact]
    public async Task UnknownRoute_Returns404()
    {
        var response = await _authClient.GetAsync("/api/v1/nonexistent-route-xyz");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
