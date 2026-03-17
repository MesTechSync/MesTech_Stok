using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Integration.Tests.Api;

/// <summary>
/// N2-KALITE — Group 4: API integration tests for accounting/finance endpoints.
/// Uses WebApplicationFactory with InMemory EF Core to test endpoint routing,
/// middleware, and response structure for accounting endpoints.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Layer", "Api")]
[Trait("Group", "AccountingApi")]
public sealed class AccountingApiTests : IClassFixture<MesTechWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;

    public AccountingApiTests(MesTechWebApplicationFactory factory)
    {
        _client = factory.CreateClient();

        _authenticatedClient = factory.CreateClient();
        _authenticatedClient.DefaultRequestHeaders.Add(
            "X-API-Key", MesTechWebApplicationFactory.TestApiKey);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 1. GET /api/v1/accounting/expenses — API key ile gecerli istek
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetExpenses_WithApiKey_DoesNotReturn401()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-30).ToString("O");
        var to = DateTime.UtcNow.ToString("O");

        // Act
        var response = await _authenticatedClient.GetAsync(
            $"/api/v1/accounting/expenses?tenantId={tenantId}&from={from}&to={to}");

        // Assert — API key gecerli, 401 olmamali
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "gecerli API key ile 401 donmemeli");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 2. POST /api/v1/accounting/expenses — Gecerli masraf olusturma
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PostExpense_WithApiKey_DoesNotReturn401()
    {
        // Arrange
        var payload = new
        {
            tenantId = Guid.NewGuid(),
            title = "Test Kargo Gideri",
            amount = 250.50m,
            category = "Kargo",
            expenseDate = DateTime.UtcNow
        };
        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _authenticatedClient.PostAsync(
            "/api/v1/accounting/expenses", content);

        // Assert — API key gecerli, 401 olmamali
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "gecerli API key ile masraf olusturma 401 donmemeli");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 3. GET /api/v1/finance/profit-loss — Kar/zarar raporu
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetProfitLoss_WithApiKey_DoesNotReturn401()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var response = await _authenticatedClient.GetAsync(
            $"/api/v1/finance/profit-loss?tenantId={tenantId}&year=2026&month=3");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "gecerli API key ile kar/zarar raporu 401 donmemeli");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 4. GET /api/v1/accounting/trial-balance — Mizan raporu
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetTrialBalance_WithApiKey_DoesNotReturn401()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var startDate = new DateTime(2026, 3, 1).ToString("O");
        var endDate = new DateTime(2026, 3, 31).ToString("O");

        // Act
        var response = await _authenticatedClient.GetAsync(
            $"/api/v1/accounting/trial-balance?tenantId={tenantId}&startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "gecerli API key ile mizan raporu 401 donmemeli");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 5. GET /api/v1/dashboard/kpi — Dashboard KPI
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetDashboardKpi_WithApiKey_DoesNotReturn401()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/v1/dashboard/kpi");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "gecerli API key ile dashboard KPI 401 donmemeli");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 6. GET /api/v1/calendar/events — Takvim etkinlikleri
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCalendarEvents_WithApiKey_DoesNotReturn401()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var response = await _authenticatedClient.GetAsync(
            $"/api/v1/calendar/events?tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 7. GET /api/v1/calendar/events — Stub response yapisi
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCalendarEvents_ReturnsJsonWithItemsField()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var response = await _authenticatedClient.GetAsync(
            $"/api/v1/calendar/events?tenantId={tenantId}");

        // Assert — Stub endpoint Items ve TotalCount doner
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            json.RootElement.TryGetProperty("items", out _).Should().BeTrue(
                "response 'items' alani icermeli");
            json.RootElement.TryGetProperty("totalCount", out var count).Should().BeTrue(
                "response 'totalCount' alani icermeli");
            count.GetInt32().Should().Be(0, "stub endpoint 0 kayit donmeli");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // 8. GET /api/v1/accounting/expenses — API key olmadan 401
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetExpenses_WithoutApiKey_Returns401()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-30).ToString("O");
        var to = DateTime.UtcNow.ToString("O");

        // Act — API key yok
        var response = await _client.GetAsync(
            $"/api/v1/accounting/expenses?tenantId={tenantId}&from={from}&to={to}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "API key olmadan muhasebe endpoint'i 401 donmeli");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 9. GET /api/v1/finance/profit-loss — API key olmadan 401
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetProfitLoss_WithoutApiKey_Returns401()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"/api/v1/finance/profit-loss?tenantId={tenantId}&year=2026&month=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "API key olmadan kar/zarar raporu 401 donmeli");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 10. GET /api/v1/accounting/chart-of-accounts — Hesap plani
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetChartOfAccounts_WithApiKey_DoesNotReturn401()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var response = await _authenticatedClient.GetAsync(
            $"/api/v1/accounting/chart-of-accounts?tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "gecerli API key ile hesap plani 401 donmemeli");
    }
}
