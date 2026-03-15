using System.Globalization;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;
using Microsoft.Extensions.Logging;

using InvoiceEntity = MesTech.Domain.Entities.Invoice;

namespace MesTech.Infrastructure.Integration.ERP.BizimHesap;

/// <summary>
/// BizimHesap ERP adapter — syncs invoices, expenses, and counterparties to BizimHesap REST API.
/// Base URL: configurable via ERP:BizimHesap:BaseUrl (default: "https://api.bizimhesap.com/v1/").
/// Auth: API Key in "X-BizimHesap-ApiKey" header via <see cref="BizimHesapApiClient"/>.
/// Simpler than Logo/Parasut — standard REST with JSON (not JSON:API).
/// </summary>
public sealed class BizimHesapERPAdapter : IERPAdapter
{
    private readonly BizimHesapApiClient _apiClient;
    private readonly ILogger<BizimHesapERPAdapter> _logger;

    public string ERPName => "BizimHesap";

    public BizimHesapERPAdapter(
        BizimHesapApiClient apiClient,
        ILogger<BizimHesapERPAdapter> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[BizimHesapERPAdapter] Testing connection");

        try
        {
            var response = await _apiClient.GetAsync("companies/me", ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[BizimHesapERPAdapter] Connection test passed (200 OK)");
                return true;
            }

            var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct);
            _logger.LogWarning(
                "[BizimHesapERPAdapter] Connection test failed: {Status} — {Error}",
                response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] Connection test exception");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task SyncInvoicesAsync(IReadOnlyList<InvoiceEntity> invoices, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoices);

        _logger.LogInformation("[BizimHesapERPAdapter] Syncing {Count} invoices", invoices.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var invoice in invoices)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var payload = BizimHesapMappingProfile.MapInvoice(invoice);
                var response = await _apiClient.PostJsonAsync("invoices", payload, ct);

                if (response.IsSuccessStatusCode)
                {
                    successCount++;
                    _logger.LogDebug(
                        "[BizimHesapERPAdapter] Invoice {InvoiceNumber} synced successfully",
                        invoice.InvoiceNumber);
                }
                else
                {
                    failCount++;
                    var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct);
                    _logger.LogWarning(
                        "[BizimHesapERPAdapter] Invoice {InvoiceNumber} sync failed: {Status} — {Error}",
                        invoice.InvoiceNumber, response.StatusCode, errorBody);
                }
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex,
                    "[BizimHesapERPAdapter] Failed to sync invoice {InvoiceNumber}",
                    invoice.InvoiceNumber);
            }
        }

        _logger.LogInformation(
            "[BizimHesapERPAdapter] Invoice sync complete: {Success} succeeded, {Failed} failed",
            successCount, failCount);
    }

    /// <inheritdoc/>
    public async Task SyncExpensesAsync(IReadOnlyList<AccountingExpenseDto> expenses, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(expenses);

        _logger.LogInformation("[BizimHesapERPAdapter] Syncing {Count} expenses", expenses.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var expense in expenses)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var payload = BizimHesapMappingProfile.MapExpense(expense);
                var response = await _apiClient.PostJsonAsync("expenses", payload, ct);

                if (response.IsSuccessStatusCode)
                {
                    successCount++;
                    _logger.LogDebug(
                        "[BizimHesapERPAdapter] Expense '{Title}' synced successfully",
                        expense.Title);
                }
                else
                {
                    failCount++;
                    var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct);
                    _logger.LogWarning(
                        "[BizimHesapERPAdapter] Expense '{Title}' sync failed: {Status} — {Error}",
                        expense.Title, response.StatusCode, errorBody);
                }
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex,
                    "[BizimHesapERPAdapter] Failed to sync expense {Title}",
                    expense.Title);
            }
        }

        _logger.LogInformation(
            "[BizimHesapERPAdapter] Expense sync complete: {Success} succeeded, {Failed} failed",
            successCount, failCount);
    }

    /// <inheritdoc/>
    public async Task SyncCounterpartiesAsync(IReadOnlyList<CounterpartyDto> parties, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(parties);

        _logger.LogInformation("[BizimHesapERPAdapter] Syncing {Count} counterparties", parties.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var party in parties)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var payload = BizimHesapMappingProfile.MapCounterparty(party);
                var response = await _apiClient.PostJsonAsync("contacts", payload, ct);

                if (response.IsSuccessStatusCode)
                {
                    successCount++;
                    _logger.LogDebug(
                        "[BizimHesapERPAdapter] Counterparty '{Name}' synced successfully",
                        party.Name);
                }
                else
                {
                    failCount++;
                    var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct);
                    _logger.LogWarning(
                        "[BizimHesapERPAdapter] Counterparty '{Name}' (VKN: {VKN}) sync failed: {Status} — {Error}",
                        party.Name, party.VKN, response.StatusCode, errorBody);
                }
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex,
                    "[BizimHesapERPAdapter] Failed to sync counterparty {Name} (VKN: {VKN})",
                    party.Name, party.VKN);
            }
        }

        _logger.LogInformation(
            "[BizimHesapERPAdapter] Counterparty sync complete: {Success} succeeded, {Failed} failed",
            successCount, failCount);
    }

    /// <inheritdoc/>
    public async Task<decimal> GetBalanceAsync(string accountCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountCode);

        _logger.LogInformation("[BizimHesapERPAdapter] Getting balance for account {AccountCode}", accountCode);

        try
        {
            var response = await _apiClient.GetAsync($"accounts/{Uri.EscapeDataString(accountCode)}", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct);
                _logger.LogWarning(
                    "[BizimHesapERPAdapter] GetBalance failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return 0m;
            }

            var accountResponse = await _apiClient.DeserializeResponseAsync<BizimHesapAccountResponse>(response, ct);

            if (accountResponse?.Balance is not null &&
                decimal.TryParse(accountResponse.Balance, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var balance))
            {
                return balance;
            }

            return 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[BizimHesapERPAdapter] GetBalance exception for account {AccountCode}", accountCode);
            return 0m;
        }
    }
}
