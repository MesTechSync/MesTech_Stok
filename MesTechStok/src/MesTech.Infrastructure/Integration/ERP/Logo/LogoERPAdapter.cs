using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

using InvoiceEntity = MesTech.Domain.Entities.Invoice;

namespace MesTech.Infrastructure.Integration.ERP.Logo;

/// <summary>
/// Logo ERP adapter — implements both legacy IERPAdapter (Accounting) and Dalga 11 IErpAdapter (Erp).
///
/// IERPAdapter: batch-based SyncInvoicesAsync, SyncExpensesAsync, SyncCounterpartiesAsync, GetBalanceAsync.
/// IErpAdapter (Dalga 11): ID-based SyncOrderAsync, SyncInvoiceAsync, GetAccountBalancesAsync, PingAsync.
///
/// Base URL: configurable via ERP:Logo:BaseUrl (default: "https://localhost/logo-rest/api/").
/// Bearer token authentication via <see cref="LogoTokenService"/>.
///
/// Logo L-Object REST API endpoints:
///   - POST /salesInvoices (invoice sync)
///   - POST /salesOrders (order sync — Dalga 12)
///   - POST /purchaseInvoices (expense sync)
///   - POST /currentAccounts (counterparty sync)
///   - GET /currentAccounts/{code}/balance (single account balance)
///   - GET /currentAccounts/balances (all account balances — Dalga 12)
///   - GET /api/v1/companies (ping/health check)
/// </summary>
public sealed class LogoERPAdapter : IERPAdapter, IErpAdapter
{
    private readonly HttpClient _httpClient;
    private readonly LogoTokenService _tokenService;
    private readonly IOrderRepository _orderRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<LogoERPAdapter> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    // ── IERPAdapter ──
    public string ERPName => "Logo";

    // ── IErpAdapter (Dalga 11) ──
    public ErpProvider Provider => ErpProvider.Logo;

    public LogoERPAdapter(
        HttpClient httpClient,
        LogoTokenService tokenService,
        IOrderRepository orderRepository,
        IInvoiceRepository invoiceRepository,
        ILogger<LogoERPAdapter> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private string BaseUrl => _tokenService.BaseUrl;

    // ═══════════════════════════════════════════════════════════════════
    // IERPAdapter — Legacy batch-based methods
    // ═══════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[LogoERPAdapter] Testing connection");

        try
        {
            await SetAuthHeaderAsync(ct);
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/v1/companies", ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[LogoERPAdapter] Connection test passed (200 OK)");
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "[LogoERPAdapter] Connection test failed: {Status} — {Error}",
                response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] Connection test exception");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task SyncInvoicesAsync(IReadOnlyList<InvoiceEntity> invoices, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoices);

        _logger.LogInformation("[LogoERPAdapter] Syncing {Count} invoices", invoices.Count);

        await SetAuthHeaderAsync(ct);

        var successCount = 0;
        var failCount = 0;

        foreach (var invoice in invoices)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var payload = LogoMappingProfile.MapInvoice(invoice);
                var result = await PostJsonAsync("salesInvoices", payload, ct);

                if (result)
                    successCount++;
                else
                    failCount++;
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex,
                    "[LogoERPAdapter] Failed to sync invoice {InvoiceNumber}",
                    invoice.InvoiceNumber);
            }
        }

        _logger.LogInformation(
            "[LogoERPAdapter] Invoice sync complete: {Success} succeeded, {Failed} failed",
            successCount, failCount);
    }

    /// <inheritdoc/>
    public async Task SyncExpensesAsync(IReadOnlyList<AccountingExpenseDto> expenses, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(expenses);

        _logger.LogInformation("[LogoERPAdapter] Syncing {Count} expenses", expenses.Count);

        await SetAuthHeaderAsync(ct);

        var successCount = 0;
        var failCount = 0;

        foreach (var expense in expenses)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var payload = LogoMappingProfile.MapExpense(expense);
                var result = await PostJsonAsync("purchaseInvoices", payload, ct);

                if (result)
                    successCount++;
                else
                    failCount++;
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex,
                    "[LogoERPAdapter] Failed to sync expense {Title}",
                    expense.Title);
            }
        }

        _logger.LogInformation(
            "[LogoERPAdapter] Expense sync complete: {Success} succeeded, {Failed} failed",
            successCount, failCount);
    }

    /// <inheritdoc/>
    public async Task SyncCounterpartiesAsync(IReadOnlyList<CounterpartyDto> parties, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(parties);

        _logger.LogInformation("[LogoERPAdapter] Syncing {Count} counterparties", parties.Count);

        await SetAuthHeaderAsync(ct);

        var successCount = 0;
        var failCount = 0;

        foreach (var party in parties)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var payload = LogoMappingProfile.MapCounterparty(party);
                var result = await PostJsonAsync("currentAccounts", payload, ct);

                if (result)
                    successCount++;
                else
                    failCount++;
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex,
                    "[LogoERPAdapter] Failed to sync counterparty {Name} (VKN: {VKN})",
                    party.Name, party.VKN);
            }
        }

        _logger.LogInformation(
            "[LogoERPAdapter] Counterparty sync complete: {Success} succeeded, {Failed} failed",
            successCount, failCount);
    }

    /// <inheritdoc/>
    public async Task<decimal> GetBalanceAsync(string accountCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountCode);

        _logger.LogInformation("[LogoERPAdapter] Getting balance for account {AccountCode}", accountCode);

        try
        {
            await SetAuthHeaderAsync(ct);

            var url = $"{BaseUrl}/currentAccounts/{Uri.EscapeDataString(accountCode)}/balance";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "[LogoERPAdapter] GetBalance failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return 0m;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var balanceResponse = JsonSerializer.Deserialize<LogoBalanceResponse>(json, JsonOptions);

            if (balanceResponse?.Balance is not null &&
                decimal.TryParse(balanceResponse.Balance, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var balance))
            {
                return balance;
            }

            return 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[LogoERPAdapter] GetBalance exception for account {AccountCode}", accountCode);
            return 0m;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpAdapter (Dalga 11) — ID-based methods
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dalga 12: Syncs a MesTech Order to Logo as a sales order.
    /// Fetches Order by ID, maps via LogoMappingProfile.MapOrder, POSTs to /salesOrders.
    /// Returns ErpSyncResult with Logo-side order reference on success.
    /// </summary>
    public async Task<ErpSyncResult> SyncOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        if (orderId == Guid.Empty)
            return ErpSyncResult.Fail("OrderId cannot be empty.");

        _logger.LogInformation(
            "[LogoERPAdapter] SyncOrderAsync — OrderId:{OrderId}", orderId);

        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order is null)
            {
                _logger.LogWarning(
                    "[LogoERPAdapter] Order not found: {OrderId}", orderId);
                return ErpSyncResult.Fail($"Order not found: {orderId}");
            }

            await SetAuthHeaderAsync(ct);

            var payload = LogoMappingProfile.MapOrder(order);
            var (success, erpId, error) = await PostJsonWithRefAsync("salesOrders", payload, ct);

            if (success)
            {
                _logger.LogInformation(
                    "[LogoERPAdapter] Order synced — OrderId:{OrderId} LogoRef:{ErpRef}",
                    orderId, erpId);
                return ErpSyncResult.Ok(erpId!);
            }

            _logger.LogWarning(
                "[LogoERPAdapter] Order sync failed — OrderId:{OrderId} Error:{Error}",
                orderId, error);
            return ErpSyncResult.Fail(error ?? "Unknown Logo API error");
        }
#pragma warning disable CA1031 // Intentional: ERP sync failure must be returned, not propagated
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[LogoERPAdapter] SyncOrderAsync exception — OrderId:{OrderId}", orderId);
            return ErpSyncResult.Fail(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Dalga 12: Syncs a MesTech Invoice to Logo as a sales invoice.
    /// Fetches Invoice by ID, maps via LogoMappingProfile.MapInvoice, POSTs to /salesInvoices.
    /// Returns ErpSyncResult with Logo-side invoice reference on success.
    /// </summary>
    public async Task<ErpSyncResult> SyncInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (invoiceId == Guid.Empty)
            return ErpSyncResult.Fail("InvoiceId cannot be empty.");

        _logger.LogInformation(
            "[LogoERPAdapter] SyncInvoiceAsync — InvoiceId:{InvoiceId}", invoiceId);

        try
        {
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
            if (invoice is null)
            {
                _logger.LogWarning(
                    "[LogoERPAdapter] Invoice not found: {InvoiceId}", invoiceId);
                return ErpSyncResult.Fail($"Invoice not found: {invoiceId}");
            }

            await SetAuthHeaderAsync(ct);

            var payload = LogoMappingProfile.MapInvoice(invoice);
            var (success, erpId, error) = await PostJsonWithRefAsync("salesInvoices", payload, ct);

            if (success)
            {
                _logger.LogInformation(
                    "[LogoERPAdapter] Invoice synced — InvoiceId:{InvoiceId} LogoRef:{ErpRef}",
                    invoiceId, erpId);
                return ErpSyncResult.Ok(erpId!);
            }

            _logger.LogWarning(
                "[LogoERPAdapter] Invoice sync failed — InvoiceId:{InvoiceId} Error:{Error}",
                invoiceId, error);
            return ErpSyncResult.Fail(error ?? "Unknown Logo API error");
        }
#pragma warning disable CA1031 // Intentional: ERP sync failure must be returned, not propagated
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[LogoERPAdapter] SyncInvoiceAsync exception — InvoiceId:{InvoiceId}", invoiceId);
            return ErpSyncResult.Fail(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Dalga 12: Retrieves all account balances from Logo.
    /// GET /currentAccounts/balances — returns list of ErpAccountDto.
    /// Returns empty list on error (graceful degradation).
    /// </summary>
    public async Task<IReadOnlyList<ErpAccountDto>> GetAccountBalancesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[LogoERPAdapter] GetAccountBalancesAsync");

        try
        {
            await SetAuthHeaderAsync(ct);

            var url = $"{BaseUrl}/currentAccounts/balances";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "[LogoERPAdapter] GetAccountBalances failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return Array.Empty<ErpAccountDto>();
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var balancesResponse = JsonSerializer.Deserialize<LogoAccountBalancesResponse>(json, JsonOptions);

            if (balancesResponse?.Accounts is null || balancesResponse.Accounts.Count == 0)
            {
                _logger.LogInformation("[LogoERPAdapter] No account balances returned from Logo");
                return Array.Empty<ErpAccountDto>();
            }

            var results = new List<ErpAccountDto>(balancesResponse.Accounts.Count);
            foreach (var account in balancesResponse.Accounts)
            {
                if (decimal.TryParse(account.Balance, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var balance))
                {
                    results.Add(new ErpAccountDto(
                        account.AccountCode,
                        account.AccountName,
                        balance,
                        account.Currency));
                }
                else
                {
                    _logger.LogDebug(
                        "[LogoERPAdapter] Could not parse balance for account {AccountCode}: '{Balance}'",
                        account.AccountCode, account.Balance);
                }
            }

            _logger.LogInformation(
                "[LogoERPAdapter] Retrieved {Count} account balances from Logo",
                results.Count);

            return results.AsReadOnly();
        }
#pragma warning disable CA1031 // Intentional: graceful degradation — return empty on error
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] GetAccountBalancesAsync exception");
            return Array.Empty<ErpAccountDto>();
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Dalga 12: Health check for Logo REST API.
    /// Delegates to TestConnectionAsync — same /api/v1/companies endpoint.
    /// </summary>
    public Task<bool> PingAsync(CancellationToken ct = default)
    {
        return TestConnectionAsync(ct);
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private async Task SetAuthHeaderAsync(CancellationToken ct)
    {
        var token = await _tokenService.GetAccessTokenAsync(ct);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<bool> PostJsonAsync<T>(string endpoint, T payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{BaseUrl}/{endpoint}", content, ct);

        if (response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync(ct);
            _logger.LogDebug(
                "[LogoERPAdapter] POST {Endpoint} succeeded: {Response}",
                endpoint, responseJson);
            return true;
        }

        var errorBody = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning(
            "[LogoERPAdapter] POST {Endpoint} failed: {Status} — {Error}",
            endpoint, response.StatusCode, errorBody);

        // Invalidate token on 401 — next call will re-authenticate
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _tokenService.InvalidateToken();
        }

        return false;
    }

    /// <summary>
    /// Dalga 12: Posts JSON payload and extracts the ERP-side record ID from response.
    /// Returns (success, erpId, errorMessage).
    /// Used by SyncOrderAsync and SyncInvoiceAsync for ErpSyncResult construction.
    /// </summary>
    private async Task<(bool Success, string? ErpId, string? Error)> PostJsonWithRefAsync<T>(
        string endpoint, T payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{BaseUrl}/{endpoint}", content, ct);

        if (response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync(ct);
            _logger.LogDebug(
                "[LogoERPAdapter] POST {Endpoint} succeeded: {Response}",
                endpoint, responseJson);

            var parsed = JsonSerializer.Deserialize<LogoCreateResponse>(responseJson, JsonOptions);
            var erpId = parsed?.Id;

            // If response doesn't contain an ID, generate a synthetic reference
            if (string.IsNullOrEmpty(erpId))
            {
                erpId = $"LOGO-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32];
            }

            return (true, erpId, null);
        }

        var errorBody = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning(
            "[LogoERPAdapter] POST {Endpoint} failed: {Status} — {Error}",
            endpoint, response.StatusCode, errorBody);

        // Invalidate token on 401 — next call will re-authenticate
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _tokenService.InvalidateToken();
        }

        return (false, null, errorBody);
    }
}
