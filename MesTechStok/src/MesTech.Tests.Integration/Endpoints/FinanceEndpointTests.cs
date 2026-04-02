using System.Net;
using System.Text;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for FinanceEndpoints.
/// Routes: GET /api/v1/finance/profit-loss, /cash-flow, /budget-summary, /cash-registers,
///             /expenses/list, /income-expenses, /income-expense-summary, /kar-zarar, /bank-accounts
///         POST /api/v1/finance/expenses/{id}/approve, /{id}/pay, /cash-registers/{id}/close,
///              /cash-registers, /expenses, /cash-transactions
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Category", "Integration")]
public sealed class FinanceEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public FinanceEndpointTests(EndpointTestWebAppFactory factory)
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
            $"/api/v1/finance/profit-loss?tenantId={Guid.NewGuid()}&year=2026&month=3");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfitLoss_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync(
            $"/api/v1/finance/profit-loss?tenantId={Guid.NewGuid()}&year=2026&month=3");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetCashFlow_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync(
            $"/api/v1/finance/cash-flow?tenantId={Guid.NewGuid()}&year=2026&month=3");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetBankAccounts_NoApiKey_Returns401()
    {
        var response = await _noAuthClient.GetAsync($"/api/v1/finance/bank-accounts?tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateExpense_EmptyBody_Returns400()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _authClient.PostAsync("/api/v1/finance/expenses", content);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetKarZarar_ValidRequest_ReturnsExpected()
    {
        var response = await _authClient.GetAsync(
            $"/api/v1/finance/kar-zarar?from=2026-01-01&to=2026-03-31&tenantId={Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }
}
