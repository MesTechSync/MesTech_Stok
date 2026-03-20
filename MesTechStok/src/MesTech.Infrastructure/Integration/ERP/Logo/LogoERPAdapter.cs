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
public sealed class LogoERPAdapter : IERPAdapter, IErpAdapter, IErpInvoiceCapable, IErpAccountCapable, IErpStockCapable, IErpWaybillCapable, IErpBankCapable
{
    private readonly HttpClient _httpClient;
    private readonly LogoTokenService _tokenService;
    private readonly IOrderRepository _orderRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<LogoERPAdapter> _logger;

    /// <summary>
    /// İ-14 R-03: Concurrency limiter — max 50 parallel requests to Logo REST API.
    /// Prevents overwhelming the Logo server which has limited connection capacity.
    /// </summary>
    private static readonly SemaphoreSlim RateLimiter = new(50, 50);

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

    // ═══════════════════════════════════════════════════════════════════
    // IErpInvoiceCapable — ISP invoice capability
    // ═══════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<ErpInvoiceResult> CreateInvoiceAsync(ErpInvoiceRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "[LogoERPAdapter] CreateInvoiceAsync — Customer:{Customer}", request.CustomerCode);

        try
        {
            await SetAuthHeaderAsync(ct);

            var payload = new LogoSalesInvoiceRequest
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Description = request.Notes,
                IssueDate = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DueDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Currency = request.Currency,
                NetTotal = request.SubTotal.ToString("F2", CultureInfo.InvariantCulture),
                TaxTotal = request.TaxTotal.ToString("F2", CultureInfo.InvariantCulture),
                GrossTotal = request.GrandTotal.ToString("F2", CultureInfo.InvariantCulture),
                CustomerName = request.CustomerName,
                CustomerTaxNumber = request.TaxId
            };

            var (success, erpId, error) = await PostJsonWithRefAsync("salesInvoices", payload, ct);

            if (success)
            {
                _logger.LogInformation(
                    "[LogoERPAdapter] Invoice created — ErpRef:{ErpRef}", erpId);
                return ErpInvoiceResult.Ok(
                    payload.InvoiceNumber,
                    erpId!,
                    DateTime.UtcNow,
                    request.GrandTotal);
            }

            return ErpInvoiceResult.Failed(error ?? "Logo API error");
        }
#pragma warning disable CA1031 // Intentional: capability failure must be returned, not propagated
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] CreateInvoiceAsync exception");
            return ErpInvoiceResult.Failed(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    public async Task<ErpInvoiceResult?> GetInvoiceAsync(string invoiceNumber, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);

        _logger.LogInformation(
            "[LogoERPAdapter] GetInvoiceAsync — InvoiceNumber:{InvoiceNumber}", invoiceNumber);

        try
        {
            await SetAuthHeaderAsync(ct);

            var url = $"{BaseUrl}/salesInvoices/{Uri.EscapeDataString(invoiceNumber)}";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[LogoERPAdapter] GetInvoice failed: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var detail = JsonSerializer.Deserialize<LogoInvoiceDetailResponse>(json, JsonOptions);

            if (detail is null)
                return null;

            var invoiceDate = DateTime.TryParse(detail.IssueDate, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed) ? parsed : DateTime.UtcNow;

            decimal.TryParse(detail.GrossTotal, NumberStyles.Any,
                CultureInfo.InvariantCulture, out var grandTotal);

            return ErpInvoiceResult.Ok(
                detail.InvoiceNumber,
                detail.ErpRef ?? detail.InvoiceNumber,
                invoiceDate,
                grandTotal,
                detail.PdfUrl);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] GetInvoiceAsync exception");
            return null;
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    public async Task<List<ErpInvoiceResult>> GetInvoicesAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[LogoERPAdapter] GetInvoicesAsync — From:{From} To:{To}",
            from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        try
        {
            await SetAuthHeaderAsync(ct);

            var fromStr = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var toStr = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var url = $"{BaseUrl}/salesInvoices?filter=DATE_ ge '{fromStr}' and DATE_ le '{toStr}'";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[LogoERPAdapter] GetInvoices failed: {Status}", response.StatusCode);
                return new List<ErpInvoiceResult>();
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var listResponse = JsonSerializer.Deserialize<LogoInvoiceListResponse>(json, JsonOptions);

            if (listResponse?.Items is null || listResponse.Items.Count == 0)
                return new List<ErpInvoiceResult>();

            var results = new List<ErpInvoiceResult>(listResponse.Items.Count);
            foreach (var item in listResponse.Items)
            {
                var invoiceDate = DateTime.TryParse(item.IssueDate, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsed) ? parsed : DateTime.UtcNow;
                decimal.TryParse(item.GrossTotal, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var grandTotal);

                results.Add(ErpInvoiceResult.Ok(
                    item.InvoiceNumber,
                    item.ErpRef ?? item.InvoiceNumber,
                    invoiceDate,
                    grandTotal,
                    item.PdfUrl));
            }

            return results;
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] GetInvoicesAsync exception");
            return new List<ErpInvoiceResult>();
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    public async Task<bool> CancelInvoiceAsync(string invoiceNumber, string reason, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);

        _logger.LogInformation(
            "[LogoERPAdapter] CancelInvoiceAsync — InvoiceNumber:{InvoiceNumber} Reason:{Reason}",
            invoiceNumber, reason);

        try
        {
            await SetAuthHeaderAsync(ct);

            var url = $"{BaseUrl}/salesInvoices/{Uri.EscapeDataString(invoiceNumber)}";
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Add("X-Cancel-Reason", reason ?? "Cancelled");

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "[LogoERPAdapter] Invoice cancelled — InvoiceNumber:{InvoiceNumber}", invoiceNumber);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "[LogoERPAdapter] CancelInvoice failed: {Status} — {Error}",
                response.StatusCode, errorBody);
            return false;
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] CancelInvoiceAsync exception");
            return false;
        }
#pragma warning restore CA1031
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpAccountCapable — ISP account capability
    // ═══════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<ErpAccountResult> CreateAccountAsync(ErpAccountRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "[LogoERPAdapter] CreateAccountAsync — Code:{Code}", request.AccountCode);

        try
        {
            await SetAuthHeaderAsync(ct);

            var payload = new LogoCurrentAccountRequest
            {
                Code = request.AccountCode,
                Title = request.CompanyName,
                TaxNumber = request.TaxId,
                AccountType = 1,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address
            };

            var (success, erpId, error) = await PostJsonWithRefAsync("currentAccounts", payload, ct);

            if (success)
            {
                _logger.LogInformation(
                    "[LogoERPAdapter] Account created — Code:{Code}", request.AccountCode);
                return ErpAccountResult.Ok(request.AccountCode, request.CompanyName, 0m);
            }

            return ErpAccountResult.Failed(error ?? "Logo API error");
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] CreateAccountAsync exception");
            return ErpAccountResult.Failed(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    public async Task<ErpAccountResult?> GetAccountAsync(string accountCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountCode);

        _logger.LogInformation(
            "[LogoERPAdapter] GetAccountAsync — Code:{Code}", accountCode);

        try
        {
            await SetAuthHeaderAsync(ct);

            var url = $"{BaseUrl}/currentAccounts/{Uri.EscapeDataString(accountCode)}";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[LogoERPAdapter] GetAccount failed: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var detail = JsonSerializer.Deserialize<LogoAccountDetailResponse>(json, JsonOptions);

            if (detail is null)
                return null;

            decimal.TryParse(detail.Balance, NumberStyles.Any,
                CultureInfo.InvariantCulture, out var balance);

            return ErpAccountResult.Ok(detail.Code, detail.Title, balance, detail.Currency);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] GetAccountAsync exception");
            return null;
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    public async Task<ErpAccountResult> UpdateAccountAsync(ErpAccountRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "[LogoERPAdapter] UpdateAccountAsync — Code:{Code}", request.AccountCode);

        try
        {
            await SetAuthHeaderAsync(ct);

            var payload = new LogoCurrentAccountRequest
            {
                Code = request.AccountCode,
                Title = request.CompanyName,
                TaxNumber = request.TaxId,
                AccountType = 1,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address
            };

            var jsonStr = JsonSerializer.Serialize(payload, JsonOptions);
            var content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
            var url = $"{BaseUrl}/currentAccounts/{Uri.EscapeDataString(request.AccountCode)}";

            var response = await _httpClient.PutAsync(url, content, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "[LogoERPAdapter] Account updated — Code:{Code}", request.AccountCode);
                return ErpAccountResult.Ok(request.AccountCode, request.CompanyName, 0m);
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "[LogoERPAdapter] UpdateAccount failed: {Status} — {Error}",
                response.StatusCode, errorBody);
            return ErpAccountResult.Failed(errorBody);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] UpdateAccountAsync exception");
            return ErpAccountResult.Failed(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    public async Task<List<ErpAccountResult>> SearchAccountsAsync(string query, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        _logger.LogInformation(
            "[LogoERPAdapter] SearchAccountsAsync — Query:{Query}", query);

        try
        {
            await SetAuthHeaderAsync(ct);

            var url = $"{BaseUrl}/currentAccounts?filter=TITLE_ contains '{Uri.EscapeDataString(query)}'";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[LogoERPAdapter] SearchAccounts failed: {Status}", response.StatusCode);
                return new List<ErpAccountResult>();
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var searchResponse = JsonSerializer.Deserialize<LogoAccountSearchResponse>(json, JsonOptions);

            if (searchResponse?.Items is null || searchResponse.Items.Count == 0)
                return new List<ErpAccountResult>();

            var results = new List<ErpAccountResult>(searchResponse.Items.Count);
            foreach (var item in searchResponse.Items)
            {
                decimal.TryParse(item.Balance, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var balance);
                results.Add(ErpAccountResult.Ok(item.Code, item.Title, balance, item.Currency));
            }

            return results;
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] SearchAccountsAsync exception");
            return new List<ErpAccountResult>();
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    public async Task<decimal> GetAccountBalanceAsync(string accountCode, CancellationToken ct = default)
    {
        // Delegate to existing legacy method
        return await GetBalanceAsync(accountCode, ct);
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpStockCapable — ISP stock capability
    // ═══════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<List<ErpStockItem>> GetStockLevelsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[LogoERPAdapter] GetStockLevelsAsync");

        try
        {
            await SetAuthHeaderAsync(ct);

            var url = $"{BaseUrl}/items?filter=ACTIVE eq 1";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[LogoERPAdapter] GetStockLevels failed: {Status}", response.StatusCode);
                return new List<ErpStockItem>();
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var listResponse = JsonSerializer.Deserialize<LogoStockListResponse>(json, JsonOptions);

            if (listResponse?.Items is null || listResponse.Items.Count == 0)
                return new List<ErpStockItem>();

            var results = new List<ErpStockItem>(listResponse.Items.Count);
            foreach (var item in listResponse.Items)
            {
                decimal? unitCost = null;
                if (!string.IsNullOrEmpty(item.UnitCost) &&
                    decimal.TryParse(item.UnitCost, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out var cost))
                {
                    unitCost = cost;
                }

                results.Add(new ErpStockItem(
                    item.Code,
                    item.Name,
                    item.Quantity,
                    item.UnitCode,
                    item.WarehouseCode,
                    unitCost));
            }

            _logger.LogInformation(
                "[LogoERPAdapter] Retrieved {Count} stock items from Logo", results.Count);
            return results;
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] GetStockLevelsAsync exception");
            return new List<ErpStockItem>();
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    public async Task<ErpStockItem?> GetStockByCodeAsync(string productCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);

        _logger.LogInformation(
            "[LogoERPAdapter] GetStockByCodeAsync — ProductCode:{ProductCode}", productCode);

        try
        {
            await SetAuthHeaderAsync(ct);

            var url = $"{BaseUrl}/items/{Uri.EscapeDataString(productCode)}";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[LogoERPAdapter] GetStockByCode failed: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var item = JsonSerializer.Deserialize<LogoStockItemResponse>(json, JsonOptions);

            if (item is null)
                return null;

            decimal? unitCost = null;
            if (!string.IsNullOrEmpty(item.UnitCost) &&
                decimal.TryParse(item.UnitCost, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var cost))
            {
                unitCost = cost;
            }

            return new ErpStockItem(
                item.Code,
                item.Name,
                item.Quantity,
                item.UnitCode,
                item.WarehouseCode,
                unitCost);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] GetStockByCodeAsync exception");
            return null;
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateStockAsync(string productCode, int quantity, string warehouseCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(warehouseCode);

        _logger.LogInformation(
            "[LogoERPAdapter] UpdateStockAsync — ProductCode:{ProductCode} Qty:{Quantity} Warehouse:{Warehouse}",
            productCode, quantity, warehouseCode);

        try
        {
            await SetAuthHeaderAsync(ct);

            var payload = new LogoStockUpdateRequest
            {
                Quantity = quantity,
                WarehouseCode = warehouseCode
            };

            var url = $"items/{Uri.EscapeDataString(productCode)}/inventory";
            return await PostJsonAsync(url, payload, ct);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] UpdateStockAsync exception");
            return false;
        }
#pragma warning restore CA1031
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpWaybillCapable — ISP waybill capability
    // ═══════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<ErpWaybillResult> CreateWaybillAsync(ErpWaybillRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "[LogoERPAdapter] CreateWaybillAsync — Customer:{Customer}", request.CustomerCode);

        try
        {
            await SetAuthHeaderAsync(ct);

            var payload = new LogoSalesDispatchRequest
            {
                CustomerCode = request.CustomerCode,
                ShippingAddress = request.ShippingAddress,
                CargoFirm = request.CargoFirm,
                TrackingNumber = request.TrackingNumber
            };

            foreach (var line in request.Lines)
            {
                payload.Lines.Add(new LogoSalesDispatchLineRequest
                {
                    ProductCode = line.ProductCode,
                    Quantity = line.Quantity,
                    UnitCode = line.UnitCode
                });
            }

            var (success, erpId, error) = await PostJsonWithRefAsync("salesDispatches", payload, ct);

            if (success)
            {
                _logger.LogInformation(
                    "[LogoERPAdapter] Waybill created — ErpRef:{ErpRef}", erpId);
                return ErpWaybillResult.Ok(erpId!, DateTime.UtcNow);
            }

            return ErpWaybillResult.Failed(error ?? "Logo API error");
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] CreateWaybillAsync exception");
            return ErpWaybillResult.Failed(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    public async Task<ErpWaybillResult?> GetWaybillAsync(string waybillNumber, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(waybillNumber);

        _logger.LogInformation(
            "[LogoERPAdapter] GetWaybillAsync — WaybillNumber:{WaybillNumber}", waybillNumber);

        try
        {
            await SetAuthHeaderAsync(ct);

            var url = $"{BaseUrl}/salesDispatches/{Uri.EscapeDataString(waybillNumber)}";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[LogoERPAdapter] GetWaybill failed: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var detail = JsonSerializer.Deserialize<LogoWaybillDetailResponse>(json, JsonOptions);

            if (detail is null)
                return null;

            var waybillDate = DateTime.TryParse(detail.WaybillDate, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed) ? parsed : DateTime.UtcNow;

            return ErpWaybillResult.Ok(detail.WaybillNumber, waybillDate);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] GetWaybillAsync exception");
            return null;
        }
#pragma warning restore CA1031
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpBankCapable — ISP bank capability
    // ═══════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<List<ErpBankTransaction>> GetTransactionsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[LogoERPAdapter] GetTransactionsAsync — From:{From} To:{To}",
            from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        try
        {
            await SetAuthHeaderAsync(ct);

            var fromStr = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var toStr = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var url = $"{BaseUrl}/bankSlips?filter=DATE_ ge '{fromStr}' and DATE_ le '{toStr}'";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[LogoERPAdapter] GetTransactions failed: {Status}", response.StatusCode);
                return new List<ErpBankTransaction>();
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var listResponse = JsonSerializer.Deserialize<LogoBankTransactionListResponse>(json, JsonOptions);

            if (listResponse?.Items is null || listResponse.Items.Count == 0)
                return new List<ErpBankTransaction>();

            var results = new List<ErpBankTransaction>(listResponse.Items.Count);
            foreach (var item in listResponse.Items)
            {
                var txDate = DateTime.TryParse(item.TransactionDate, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsed) ? parsed : DateTime.UtcNow;
                decimal.TryParse(item.Amount, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var amount);

                results.Add(new ErpBankTransaction(
                    txDate,
                    amount,
                    item.Description,
                    item.TransactionType,
                    item.Reference));
            }

            _logger.LogInformation(
                "[LogoERPAdapter] Retrieved {Count} bank transactions from Logo", results.Count);
            return results;
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] GetTransactionsAsync exception");
            return new List<ErpBankTransaction>();
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    public async Task<ErpPaymentResult> RecordPaymentAsync(ErpPaymentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "[LogoERPAdapter] RecordPaymentAsync — Account:{Account} Amount:{Amount}",
            request.AccountCode, request.Amount);

        try
        {
            await SetAuthHeaderAsync(ct);

            var payload = new LogoBankPaymentRequest
            {
                AccountCode = request.AccountCode,
                Amount = request.Amount.ToString("F2", CultureInfo.InvariantCulture),
                PaymentType = request.PaymentType,
                DueDate = request.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Description = request.Description
            };

            var (success, erpId, error) = await PostJsonWithRefAsync("bankSlips", payload, ct);

            if (success)
            {
                _logger.LogInformation(
                    "[LogoERPAdapter] Payment recorded — Ref:{Ref}", erpId);
                return ErpPaymentResult.Ok(erpId!);
            }

            return ErpPaymentResult.Failed(error ?? "Logo API error");
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LogoERPAdapter] RecordPaymentAsync exception");
            return ErpPaymentResult.Failed(ex.Message);
        }
#pragma warning restore CA1031
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
        await RateLimiter.WaitAsync(ct);
        try
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
        finally
        {
            RateLimiter.Release();
        }
    }

    /// <summary>
    /// Dalga 12: Posts JSON payload and extracts the ERP-side record ID from response.
    /// Returns (success, erpId, errorMessage).
    /// Used by SyncOrderAsync and SyncInvoiceAsync for ErpSyncResult construction.
    /// </summary>
    private async Task<(bool Success, string? ErpId, string? Error)> PostJsonWithRefAsync<T>(
        string endpoint, T payload, CancellationToken ct)
    {
        await RateLimiter.WaitAsync(ct);
        try
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
        finally
        {
            RateLimiter.Release();
        }
    }
}
