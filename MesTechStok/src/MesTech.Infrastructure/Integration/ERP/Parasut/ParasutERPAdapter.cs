using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities.EInvoice;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using InvoiceEntity = MesTech.Domain.Entities.Invoice;

namespace MesTech.Infrastructure.Integration.ERP.Parasut;

/// <summary>
/// Parasut ERP adapter — syncs invoices, expenses, and counterparties to Parasut.
/// Base URL: https://api.parasut.com/v4/{company_id}/
/// JSON:API format (application/vnd.api+json).
/// OAuth2 Client Credentials authentication via <see cref="ParasutTokenService"/>.
/// </summary>
public sealed class ParasutERPAdapter : IERPAdapter, IErpInvoiceCapable, IErpAccountCapable, IErpStockCapable, IErpBankCapable
{
    private readonly HttpClient _httpClient;
    private readonly ParasutTokenService _tokenService;
    private readonly ParasutOptions _options;
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
        ILogger<ParasutERPAdapter> logger,
        IOptions<ParasutOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new ParasutOptions();
    }

    private string BaseUrl => $"{_options.BaseUrl}/v4/{_tokenService.CompanyId}";

    /// <inheritdoc/>
    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[ParasutERPAdapter] Testing connection");

        try
        {
            await SetAuthHeaderAsync(ct).ConfigureAwait(false);
            var response = await _httpClient.GetAsync($"{BaseUrl}/accounts", ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[ParasutERPAdapter] Connection test passed (200 OK)");
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
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

        await SetAuthHeaderAsync(ct).ConfigureAwait(false);

        var successCount = 0;
        var failCount = 0;

        foreach (var invoice in invoices)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var payload = ParasutMappingProfile.MapInvoice(invoice);
                var result = await PostJsonApiAsync("sales_invoices", payload, ct).ConfigureAwait(false);

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

        await SetAuthHeaderAsync(ct).ConfigureAwait(false);

        var successCount = 0;
        var failCount = 0;

        foreach (var expense in expenses)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var payload = ParasutMappingProfile.MapExpense(expense);
                var result = await PostJsonApiAsync("purchase_bills", payload, ct).ConfigureAwait(false);

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

        await SetAuthHeaderAsync(ct).ConfigureAwait(false);

        var successCount = 0;
        var failCount = 0;

        foreach (var party in parties)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var payload = ParasutMappingProfile.MapCounterparty(party);
                var result = await PostJsonApiAsync("contacts", payload, ct).ConfigureAwait(false);

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
            await SetAuthHeaderAsync(ct).ConfigureAwait(false);

            var url = $"{BaseUrl}/accounts?filter[code]={Uri.EscapeDataString(accountCode)}";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning(
                    "[ParasutERPAdapter] GetBalance failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return 0m;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
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
        var token = await _tokenService.GetAccessTokenAsync(ct).ConfigureAwait(false);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<bool> PostJsonApiAsync<T>(string endpoint, T payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.api+json");

        var response = await _httpClient.PostAsync($"{BaseUrl}/{endpoint}", content, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogDebug(
                "[ParasutERPAdapter] POST {Endpoint} succeeded: {Response}",
                endpoint, responseJson);
            return true;
        }

        var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
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

    /// <summary>
    /// Dalga 9: Posts a JSON:API payload and returns the Parasut-side record ID on success.
    /// Returns (true, erpId) on 2xx, (false, null, errorBody) on failure.
    /// </summary>
    private async Task<(bool Success, string? ErpId, string? Error)> PostJsonApiWithRefAsync<T>(
        string endpoint, T payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.api+json");

        var response = await _httpClient.PostAsync($"{BaseUrl}/{endpoint}", content, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogDebug(
                "[ParasutERPAdapter] POST {Endpoint} succeeded: {Response}",
                endpoint, responseJson);

            var parsed = JsonSerializer.Deserialize<ParasutJsonApiResponse>(responseJson, JsonOptions);
            var erpId = parsed?.Data?.Id;
            return (true, erpId, null);
        }

        var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning(
            "[ParasutERPAdapter] POST {Endpoint} failed: {Status} — {Error}",
            endpoint, response.StatusCode, errorBody);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _tokenService.InvalidateToken();
        }

        return (false, null, errorBody);
    }

    // ── Dalga 9 Extensions ────────────────────────────────────────────

    /// <summary>
    /// Dalga 9: Sends an EInvoiceDocument to Parasut as a sales_invoice.
    /// EXTEND — existing IERPAdapter methods are not modified.
    /// Returns ErpSyncResult with the Parasut record ID on success.
    /// </summary>
    public async Task<ErpSyncResult> SyncEInvoiceAsync(
        EInvoiceDocument invoice, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        _logger.LogInformation(
            "[ParasutERPAdapter] SyncEInvoiceAsync — ETTN:{EttnNo} Buyer:{Buyer} Amount:{Amount}",
            invoice.EttnNo, invoice.BuyerTitle, invoice.PayableAmount);

        try
        {
            await SetAuthHeaderAsync(ct).ConfigureAwait(false);

            var payload = ParasutMappingProfile.MapEInvoice(invoice);
            var (success, erpId, error) = await PostJsonApiWithRefAsync("sales_invoices", payload, ct).ConfigureAwait(false);

            if (success)
            {
                _logger.LogInformation(
                    "[ParasutERPAdapter] E-invoice synced — ETTN:{EttnNo} ParasutId:{ErpId}",
                    invoice.EttnNo, erpId);
                return new ErpSyncResult(true, erpId, null);
            }

            _logger.LogWarning(
                "[ParasutERPAdapter] E-invoice sync failed — ETTN:{EttnNo} Error:{Error}",
                invoice.EttnNo, error);
            return new ErpSyncResult(false, null, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ParasutERPAdapter] SyncEInvoiceAsync exception — ETTN:{EttnNo}",
                invoice.EttnNo);
            return new ErpSyncResult(false, null, ex.Message);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // ISP Capability Implementations (İ-14 C-01)
    // ══════════════════════════════════════════════════════════════

    // ── IErpInvoiceCapable ──────────────────────────────────────

    async Task<ErpInvoiceResult> IErpInvoiceCapable.CreateInvoiceAsync(ErpInvoiceRequest request, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct).ConfigureAwait(false);
        var issueDate = DateTime.UtcNow;
        var payload = new
        {
            data = new
            {
                type = "sales_invoices",
                attributes = new
                {
                    item_type = "invoice",
                    description = request.Notes ?? "",
                    issue_date = issueDate.ToString("yyyy-MM-dd"),
                    currency = request.Currency,
                    net_total = request.SubTotal.ToString("F2", CultureInfo.InvariantCulture),
                    gross_total = request.GrandTotal.ToString("F2", CultureInfo.InvariantCulture)
                }
            }
        };
        var (success, erpId, error) = await PostJsonApiWithRefAsync("sales_invoices", payload, ct).ConfigureAwait(false);
        return success
            ? ErpInvoiceResult.Ok(erpId ?? "", erpId ?? "", issueDate, request.GrandTotal)
            : ErpInvoiceResult.Failed(error ?? "Unknown error");
    }

    async Task<ErpInvoiceResult?> IErpInvoiceCapable.GetInvoiceAsync(string invoiceNumber, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct).ConfigureAwait(false);
        var url = $"{BaseUrl}/sales_invoices?filter[number]={Uri.EscapeDataString(invoiceNumber)}";
        var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("[ParasutERPAdapter] GetInvoice failed: {Status} — {Error}",
                (int)response.StatusCode, errorBody);
            return null;
        }
        return ErpInvoiceResult.Ok(invoiceNumber, "", DateTime.UtcNow, 0m);
    }

    async Task<List<ErpInvoiceResult>> IErpInvoiceCapable.GetInvoicesAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct).ConfigureAwait(false);
        var url = $"{BaseUrl}/sales_invoices?filter[issue_date]={from:yyyy-MM-dd}..{to:yyyy-MM-dd}";
        var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("[ParasutERPAdapter] GetInvoices failed: {Status} — {Error}",
                (int)response.StatusCode, errorBody);
            return new List<ErpInvoiceResult>();
        }
        // Parse JSON:API response
        return new List<ErpInvoiceResult>();
    }

    async Task<bool> IErpInvoiceCapable.CancelInvoiceAsync(string invoiceNumber, string reason, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct).ConfigureAwait(false);
        var url = $"{BaseUrl}/sales_invoices/{Uri.EscapeDataString(invoiceNumber)}";
        var response = await _httpClient.DeleteAsync(url, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("[ParasutERPAdapter] CancelInvoice failed: {Status} — {Error}",
                (int)response.StatusCode, errorBody);
        }
        return response.IsSuccessStatusCode;
    }

    // ── IErpAccountCapable ──────────────────────────────────────

    async Task<ErpAccountResult> IErpAccountCapable.CreateAccountAsync(ErpAccountRequest request, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct).ConfigureAwait(false);
        var payload = new
        {
            data = new
            {
                type = "contacts",
                attributes = new
                {
                    name = request.CompanyName,
                    tax_number = request.TaxId ?? "",
                    tax_office = request.TaxOffice ?? "",
                    address = request.Address ?? "",
                    city = request.City ?? "",
                    phone = request.Phone ?? "",
                    email = request.Email ?? ""
                }
            }
        };
        var (success, erpId, error) = await PostJsonApiWithRefAsync("contacts", payload, ct).ConfigureAwait(false);
        return success
            ? ErpAccountResult.Ok(request.AccountCode, request.CompanyName, 0m)
            : ErpAccountResult.Failed(error ?? "Unknown error");
    }

    async Task<ErpAccountResult?> IErpAccountCapable.GetAccountAsync(string accountCode, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct).ConfigureAwait(false);
        var url = $"{BaseUrl}/contacts?filter[code]={Uri.EscapeDataString(accountCode)}";
        var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("[ParasutERPAdapter] GetAccount failed: {Status} — {Error}",
                (int)response.StatusCode, errorBody);
            return null;
        }
        return ErpAccountResult.Ok(accountCode, "", 0m);
    }

    async Task<ErpAccountResult> IErpAccountCapable.UpdateAccountAsync(ErpAccountRequest request, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct).ConfigureAwait(false);
        // First find the contact
        var existing = await ((IErpAccountCapable)this).GetAccountAsync(request.AccountCode, ct).ConfigureAwait(false);
        if (existing is null)
            return ErpAccountResult.Failed($"Contact not found for account code {request.AccountCode}");

        var payload = new
        {
            data = new
            {
                type = "contacts",
                attributes = new
                {
                    name = request.CompanyName,
                    address = request.Address ?? "",
                    city = request.City ?? "",
                    phone = request.Phone ?? "",
                    email = request.Email ?? ""
                }
            }
        };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.api+json");
        var response = await _httpClient.PatchAsync($"{BaseUrl}/contacts/{Uri.EscapeDataString(request.AccountCode)}", content, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("[ParasutERPAdapter] UpdateAccount failed: {Status} — {Error}",
                (int)response.StatusCode, errorBody);
            return ErpAccountResult.Failed($"Update failed with status {response.StatusCode}");
        }
        return ErpAccountResult.Ok(request.AccountCode, request.CompanyName, 0m);
    }

    async Task<List<ErpAccountResult>> IErpAccountCapable.SearchAccountsAsync(string query, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct).ConfigureAwait(false);
        var url = $"{BaseUrl}/contacts?filter[name]={Uri.EscapeDataString(query)}";
        var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("[ParasutERPAdapter] SearchAccounts failed: {Status} — {Error}",
                (int)response.StatusCode, errorBody);
            return new List<ErpAccountResult>();
        }
        return new List<ErpAccountResult>();
    }

    async Task<decimal> IErpAccountCapable.GetAccountBalanceAsync(string accountCode, CancellationToken ct)
    {
        // Delegate to existing GetBalanceAsync
        return await GetBalanceAsync(accountCode, ct).ConfigureAwait(false);
    }

    // ── IErpStockCapable ────────────────────────────────────────

    async Task<List<ErpStockItem>> IErpStockCapable.GetStockLevelsAsync(CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct).ConfigureAwait(false);
        var url = $"{BaseUrl}/products?include=inventory_levels&page[size]=250";
        var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("[ParasutERPAdapter] GetStockLevels failed: {Status} — {Error}",
                (int)response.StatusCode, errorBody);
            return new List<ErpStockItem>();
        }
        return new List<ErpStockItem>();
    }

    async Task<ErpStockItem?> IErpStockCapable.GetStockByCodeAsync(string productCode, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct).ConfigureAwait(false);
        var url = $"{BaseUrl}/products?filter[code]={Uri.EscapeDataString(productCode)}";
        var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("[ParasutERPAdapter] GetStockByCode failed: {Status} — {Error}",
                (int)response.StatusCode, errorBody);
            return null;
        }
        return null; // Parse JSON:API response
    }

    async Task<bool> IErpStockCapable.UpdateStockAsync(string productCode, int quantity, string warehouseCode, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct).ConfigureAwait(false);
        var payload = new
        {
            data = new
            {
                type = "stock_movements",
                attributes = new
                {
                    product_code = productCode,
                    quantity,
                    warehouse_code = warehouseCode
                }
            }
        };
        return await PostJsonApiAsync("stock_movements", payload, ct).ConfigureAwait(false);
    }

    // ── IErpBankCapable ─────────────────────────────────────────

    async Task<List<ErpBankTransaction>> IErpBankCapable.GetTransactionsAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct).ConfigureAwait(false);
        var url = $"{BaseUrl}/bank_transactions?filter[date]={from:yyyy-MM-dd}..{to:yyyy-MM-dd}";
        var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("[ParasutERPAdapter] GetTransactions failed: {Status} — {Error}",
                (int)response.StatusCode, errorBody);
            return new List<ErpBankTransaction>();
        }
        return new List<ErpBankTransaction>();
    }

    async Task<ErpPaymentResult> IErpBankCapable.RecordPaymentAsync(ErpPaymentRequest request, CancellationToken ct)
    {
        await SetAuthHeaderAsync(ct).ConfigureAwait(false);
        var payload = new
        {
            data = new
            {
                type = "payments",
                attributes = new
                {
                    account_id = request.AccountCode,
                    amount = request.Amount.ToString("F2", CultureInfo.InvariantCulture),
                    date = request.DueDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    description = request.Description ?? ""
                }
            }
        };
        var (success, erpId, error) = await PostJsonApiWithRefAsync("payments", payload, ct).ConfigureAwait(false);
        return success
            ? ErpPaymentResult.Ok(erpId ?? "")
            : ErpPaymentResult.Failed(error ?? "Unknown error");
    }
}
