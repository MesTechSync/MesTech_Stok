using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;
using Microsoft.Extensions.Logging;

using InvoiceEntity = MesTech.Domain.Entities.Invoice;

namespace MesTech.Infrastructure.Integration.ERP.Parasut;

/// <summary>
/// Parasut ERP adapter — syncs invoices, expenses, and counterparties to Parasut.
/// Base URL: https://api.parasut.com/v4/{company_id}/
/// JSON:API format (application/vnd.api+json).
/// OAuth2 Client Credentials authentication via <see cref="ParasutTokenService"/>.
/// </summary>
public sealed class ParasutERPAdapter : IERPAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ParasutTokenService _tokenService;
    private readonly ILogger<ParasutERPAdapter> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public string ERPName => "Parasut";

    public ParasutERPAdapter(
        HttpClient httpClient,
        ParasutTokenService tokenService,
        ILogger<ParasutERPAdapter> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private string BaseUrl => $"https://api.parasut.com/v4/{_tokenService.CompanyId}";

    /// <inheritdoc/>
    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[ParasutERPAdapter] Testing connection");

        try
        {
            await SetAuthHeaderAsync(ct);
            var response = await _httpClient.GetAsync($"{BaseUrl}/accounts", ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[ParasutERPAdapter] Connection test passed (200 OK)");
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "[ParasutERPAdapter] Connection test failed: {Status} — {Error}",
                response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ParasutERPAdapter] Connection test exception");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task SyncInvoicesAsync(IReadOnlyList<InvoiceEntity> invoices, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoices);

        _logger.LogInformation("[ParasutERPAdapter] Syncing {Count} invoices", invoices.Count);

        await SetAuthHeaderAsync(ct);

        var successCount = 0;
        var failCount = 0;

        foreach (var invoice in invoices)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var payload = ParasutMappingProfile.MapInvoice(invoice);
                var result = await PostJsonApiAsync("sales_invoices", payload, ct);

                if (result)
                    successCount++;
                else
                    failCount++;
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex,
                    "[ParasutERPAdapter] Failed to sync invoice {InvoiceNumber}",
                    invoice.InvoiceNumber);
            }
        }

        _logger.LogInformation(
            "[ParasutERPAdapter] Invoice sync complete: {Success} succeeded, {Failed} failed",
            successCount, failCount);
    }

    /// <inheritdoc/>
    public async Task SyncExpensesAsync(IReadOnlyList<AccountingExpenseDto> expenses, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(expenses);

        _logger.LogInformation("[ParasutERPAdapter] Syncing {Count} expenses", expenses.Count);

        await SetAuthHeaderAsync(ct);

        var successCount = 0;
        var failCount = 0;

        foreach (var expense in expenses)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var payload = ParasutMappingProfile.MapExpense(expense);
                var result = await PostJsonApiAsync("purchase_bills", payload, ct);

                if (result)
                    successCount++;
                else
                    failCount++;
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex,
                    "[ParasutERPAdapter] Failed to sync expense {Title}",
                    expense.Title);
            }
        }

        _logger.LogInformation(
            "[ParasutERPAdapter] Expense sync complete: {Success} succeeded, {Failed} failed",
            successCount, failCount);
    }

    /// <inheritdoc/>
    public async Task SyncCounterpartiesAsync(IReadOnlyList<CounterpartyDto> parties, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(parties);

        _logger.LogInformation("[ParasutERPAdapter] Syncing {Count} counterparties", parties.Count);

        await SetAuthHeaderAsync(ct);

        var successCount = 0;
        var failCount = 0;

        foreach (var party in parties)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var payload = ParasutMappingProfile.MapCounterparty(party);
                var result = await PostJsonApiAsync("contacts", payload, ct);

                if (result)
                    successCount++;
                else
                    failCount++;
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex,
                    "[ParasutERPAdapter] Failed to sync counterparty {Name} (VKN: {VKN})",
                    party.Name, party.VKN);
            }
        }

        _logger.LogInformation(
            "[ParasutERPAdapter] Counterparty sync complete: {Success} succeeded, {Failed} failed",
            successCount, failCount);
    }

    /// <inheritdoc/>
    public async Task<decimal> GetBalanceAsync(string accountCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountCode);

        _logger.LogInformation("[ParasutERPAdapter] Getting balance for account {AccountCode}", accountCode);

        try
        {
            await SetAuthHeaderAsync(ct);

            var url = $"{BaseUrl}/accounts?filter[code]={Uri.EscapeDataString(accountCode)}";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "[ParasutERPAdapter] GetBalance failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return 0m;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var accountsResponse = JsonSerializer.Deserialize<ParasutAccountsResponse>(json, JsonOptions);

            if (accountsResponse?.Data is null || accountsResponse.Data.Count == 0)
            {
                _logger.LogWarning(
                    "[ParasutERPAdapter] No account found for code {AccountCode}", accountCode);
                return 0m;
            }

            var attrs = accountsResponse.Data[0].Attributes;
            if (attrs?.Balance is not null &&
                decimal.TryParse(attrs.Balance, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var balance))
            {
                return balance;
            }

            return 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ParasutERPAdapter] GetBalance exception for account {AccountCode}", accountCode);
            return 0m;
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private async Task SetAuthHeaderAsync(CancellationToken ct)
    {
        var token = await _tokenService.GetAccessTokenAsync(ct);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<bool> PostJsonApiAsync<T>(string endpoint, T payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.api+json");

        var response = await _httpClient.PostAsync($"{BaseUrl}/{endpoint}", content, ct);

        if (response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync(ct);
            _logger.LogDebug(
                "[ParasutERPAdapter] POST {Endpoint} succeeded: {Response}",
                endpoint, responseJson);
            return true;
        }

        var errorBody = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning(
            "[ParasutERPAdapter] POST {Endpoint} failed: {Status} — {Error}",
            endpoint, response.StatusCode, errorBody);

        // Invalidate token on 401 — next call will re-authenticate
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _tokenService.InvalidateToken();
        }

        return false;
    }
}
