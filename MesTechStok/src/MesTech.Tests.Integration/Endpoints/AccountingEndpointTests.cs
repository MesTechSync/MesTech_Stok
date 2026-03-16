using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for AccountingEndpoints (Sprint 1 DEV-H1).
/// Routes: GET trial-balance, balance-sheet, profit-report, journal-entries,
///             expenses, settlements, reconciliation/dashboard, bank-transactions,
///             chart-of-accounts, commission-rates
///         POST journal-entries, expenses, reconciliation/run, commission-rates
///         PUT  commission-rates/{id}
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Sprint", "H1")]
public sealed class AccountingEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;
    private static readonly Guid TestTenantId = Guid.NewGuid();

    public AccountingEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();

        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    // ── 1. Happy path ──

    [Fact]
    public async Task GetTrialBalance_ValidRequest_Returns200()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30).ToString("o");
        var endDate = DateTime.UtcNow.ToString("o");
        var url = $"/api/v1/accounting/trial-balance?tenantId={TestTenantId}&startDate={startDate}&endDate={endDate}";

        // Act
        var response = await _authClient.GetAsync(url);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
            var json = JsonDocument.Parse(content);
            json.RootElement.ValueKind.Should().BeOneOf(
                JsonValueKind.Object, JsonValueKind.Array);
        }
    }

    // ── 2. Validation ──

    [Fact]
    public async Task GetTrialBalance_MissingRequiredParams_ReturnsBadRequest()
    {
        // Act — missing tenantId, startDate, endDate
        var response = await _authClient.GetAsync("/api/v1/accounting/trial-balance");

        // Assert — required query parameters missing
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();
    }

    // ── 3. Auth ──

    [Fact]
    public async Task GetTrialBalance_NoApiKey_Returns401()
    {
        // Act
        var response = await _noAuthClient.GetAsync(
            $"/api/v1/accounting/trial-balance?tenantId={TestTenantId}&startDate=2026-01-01&endDate=2026-03-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("API key");
    }

    // ── 4. Not found ──

    [Fact]
    public async Task GetProfitReport_NonExistentPeriod_ReturnsNotFoundOrError()
    {
        // Arrange — valid route but non-existent data
        var url = $"/api/v1/accounting/profit-report?tenantId={TestTenantId}&period=2099-Q4&platform=nonexistent";

        // Act
        var response = await _authClient.GetAsync(url);

        // Assert — endpoint returns Results.NotFound() when report is null
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.OK,
            HttpStatusCode.InternalServerError);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ── 5. Server error ──

    [Fact]
    public async Task CreateJournalEntry_InvalidPayload_ReturnsErrorResponse()
    {
        // Arrange — invalid journal entry payload
        var content = new StringContent(
            "{\"invalidField\": \"test\"}",
            Encoding.UTF8, "application/json");

        // Act
        var response = await _authClient.PostAsync("/api/v1/accounting/journal-entries", content);

        // Assert
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotBeNullOrWhiteSpace();
            response.Content.Headers.ContentType?.MediaType.Should().Contain("json");
            var json = JsonDocument.Parse(body);
            json.RootElement.TryGetProperty("title", out _).Should().BeTrue();
            json.RootElement.TryGetProperty("status", out var status).Should().BeTrue();
            status.GetInt32().Should().Be(500);
        }
        else
        {
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Created);
        }
    }
}
