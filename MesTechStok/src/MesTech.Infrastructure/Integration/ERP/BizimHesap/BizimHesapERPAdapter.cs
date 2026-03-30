using System.Globalization;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using InvoiceEntity = MesTech.Domain.Entities.Invoice;

namespace MesTech.Infrastructure.Integration.ERP.BizimHesap;

/// <summary>
/// BizimHesap ERP adapter — syncs invoices, expenses, and counterparties to BizimHesap REST API.
/// Base URL: configurable via ERP:BizimHesap:BaseUrl (default: "https://api.bizimhesap.com/v1/").
/// Auth: API Key in "X-BizimHesap-ApiKey" header via <see cref="BizimHesapApiClient"/>.
/// Simpler than Logo/Parasut — standard REST with JSON (not JSON:API).
/// </summary>
public sealed class BizimHesapERPAdapter : IERPAdapter, IErpAdapter, IErpInvoiceCapable, IErpAccountCapable, IErpStockCapable, IErpPriceCapable, IErpWaybillCapable, IErpBankCapable
{
    private readonly BizimHesapApiClient _apiClient;
    private readonly IOrderRepository _orderRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<BizimHesapERPAdapter> _logger;

    public string ERPName => "BizimHesap";
    public ErpProvider Provider => ErpProvider.BizimHesap;

    public BizimHesapERPAdapter(
        BizimHesapApiClient apiClient,
        IOrderRepository orderRepository,
        IInvoiceRepository invoiceRepository,
        ILogger<BizimHesapERPAdapter> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[BizimHesapERPAdapter] Testing connection");

        try
        {
            var response = await _apiClient.GetAsync("companies/me", ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[BizimHesapERPAdapter] Connection test passed (200 OK)");
                return true;
            }

            var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
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

    // IErpAdapter (Dalga 11)
    public async Task<ErpSyncResult> SyncOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        if (orderId == Guid.Empty) return ErpSyncResult.Fail("OrderId empty");

        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId, ct).ConfigureAwait(false);
            if (order is null) return ErpSyncResult.Fail("Not found");

            var payload = new
            {
                orderNumber = order.OrderNumber,
                orderDate = order.OrderDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                totalAmount = order.TotalAmount
            };

            var response = await _apiClient.PostJsonAsync("api/v1/orders", payload, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var r = await _apiClient.DeserializeResponseAsync<BizimHesapSyncResponse>(response, ct).ConfigureAwait(false);
                return ErpSyncResult.Ok(r?.Reference ?? "");
            }

            return ErpSyncResult.Fail("HTTP " + (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] SyncOrder error");
            return ErpSyncResult.Fail(ex.Message);
        }
    }

    public async Task<ErpSyncResult> SyncInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (invoiceId == Guid.Empty) return ErpSyncResult.Fail("InvoiceId empty");

        try
        {
            var inv = await _invoiceRepository.GetByIdAsync(invoiceId).ConfigureAwait(false);
            if (inv is null) return ErpSyncResult.Fail("Not found");

            var payload = new
            {
                invoiceNumber = inv.InvoiceNumber,
                invoiceDate = inv.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                totalAmount = inv.GrandTotal
            };

            var response = await _apiClient.PostJsonAsync("api/v1/invoices", payload, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var r = await _apiClient.DeserializeResponseAsync<BizimHesapSyncResponse>(response, ct).ConfigureAwait(false);
                return ErpSyncResult.Ok(r?.Reference ?? "");
            }

            return ErpSyncResult.Fail("HTTP " + (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] SyncInvoice error");
            return ErpSyncResult.Fail(ex.Message);
        }
    }

    public async Task<IReadOnlyList<ErpAccountDto>> GetAccountBalancesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _apiClient.GetAsync("api/v1/contacts", ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return Array.Empty<ErpAccountDto>();

            var items = await _apiClient.DeserializeResponseAsync<List<BizimHesapAccountResponse>>(response, ct).ConfigureAwait(false);
            if (items is null) return Array.Empty<ErpAccountDto>();

            return items.Select(a => new ErpAccountDto(
                a.Code ?? "",
                a.Code ?? "",
                decimal.TryParse(a.Balance, NumberStyles.Any, CultureInfo.InvariantCulture, out var b) ? b : 0m,
                a.Currency ?? "TRY")).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] GetAccountBalances error");
            return Array.Empty<ErpAccountDto>();
        }
    }

    public Task<bool> PingAsync(CancellationToken ct = default) => TestConnectionAsync(ct);

    private sealed class BizimHesapSyncResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("reference")]
        public string? Reference { get; set; }
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
                var response = await _apiClient.PostJsonAsync("invoices", payload, ct).ConfigureAwait(false);

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
                    var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
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
                var response = await _apiClient.PostJsonAsync("expenses", payload, ct).ConfigureAwait(false);

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
                    var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
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
                var response = await _apiClient.PostJsonAsync("contacts", payload, ct).ConfigureAwait(false);

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
                    var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
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
            var response = await _apiClient.GetAsync($"accounts/{Uri.EscapeDataString(accountCode)}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
                _logger.LogWarning(
                    "[BizimHesapERPAdapter] GetBalance failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return 0m;
            }

            var accountResponse = await _apiClient.DeserializeResponseAsync<BizimHesapAccountResponse>(response, ct).ConfigureAwait(false);

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

    // ── IErpInvoiceCapable ─────────────────────────────────────────────

    /// <inheritdoc/>
    async Task<ErpInvoiceResult> IErpInvoiceCapable.CreateInvoiceAsync(ErpInvoiceRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[BizimHesapERPAdapter] Creating invoice for customer {Customer}", request.CustomerCode);

        try
        {
            var payload = new
            {
                customerCode = request.CustomerCode,
                customerName = request.CustomerName,
                taxId = request.TaxId,
                currency = request.Currency,
                notes = request.Notes,
                subTotal = request.SubTotal.ToString("F2", CultureInfo.InvariantCulture),
                taxTotal = request.TaxTotal.ToString("F2", CultureInfo.InvariantCulture),
                grandTotal = request.GrandTotal.ToString("F2", CultureInfo.InvariantCulture),
                lines = request.Lines.Select(l => new
                {
                    productCode = l.ProductCode,
                    productName = l.ProductName,
                    quantity = l.Quantity,
                    unitPrice = l.UnitPrice.ToString("F2", CultureInfo.InvariantCulture),
                    taxRate = l.TaxRate,
                    discountAmount = l.DiscountAmount?.ToString("F2", CultureInfo.InvariantCulture)
                }).ToList()
            };

            var response = await _apiClient.PostJsonAsync("api/v1/invoices", payload, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
                _logger.LogWarning("[BizimHesapERPAdapter] CreateInvoice failed: {Status} — {Error}", response.StatusCode, errorBody);
                return ErpInvoiceResult.Failed($"HTTP {(int)response.StatusCode}: {errorBody}");
            }

            var result = await _apiClient.DeserializeResponseAsync<BizimHesapInvoiceResponse>(response, ct).ConfigureAwait(false);
            return ErpInvoiceResult.Ok(
                result?.InvoiceNumber ?? string.Empty,
                result?.Id ?? string.Empty,
                result?.InvoiceDate ?? DateTime.UtcNow,
                request.GrandTotal,
                result?.PdfUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] CreateInvoice exception");
            return ErpInvoiceResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc/>
    async Task<ErpInvoiceResult?> IErpInvoiceCapable.GetInvoiceAsync(string invoiceNumber, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        _logger.LogInformation("[BizimHesapERPAdapter] Getting invoice {InvoiceNumber}", invoiceNumber);

        try
        {
            var response = await _apiClient.GetAsync($"api/v1/invoices/{Uri.EscapeDataString(invoiceNumber)}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
                _logger.LogWarning("[BizimHesapERPAdapter] GetInvoice failed: {Status} — {Error}", response.StatusCode, errorBody);
                return null;
            }

            var result = await _apiClient.DeserializeResponseAsync<BizimHesapInvoiceResponse>(response, ct).ConfigureAwait(false);
            if (result is null) return null;

            return ErpInvoiceResult.Ok(
                result.InvoiceNumber ?? invoiceNumber,
                result.Id ?? string.Empty,
                result.InvoiceDate ?? DateTime.MinValue,
                decimal.TryParse(result.GrandTotal, NumberStyles.Any, CultureInfo.InvariantCulture, out var total) ? total : 0m,
                result.PdfUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] GetInvoice exception for {InvoiceNumber}", invoiceNumber);
            return null;
        }
    }

    /// <inheritdoc/>
    async Task<List<ErpInvoiceResult>> IErpInvoiceCapable.GetInvoicesAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        _logger.LogInformation("[BizimHesapERPAdapter] Listing invoices from {From} to {To}", from, to);

        try
        {
            var startDate = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var endDate = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var response = await _apiClient.GetAsync($"api/v1/invoices?startDate={startDate}&endDate={endDate}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
                _logger.LogWarning("[BizimHesapERPAdapter] GetInvoices failed: {Status} — {Error}", response.StatusCode, errorBody);
                return [];
            }

            var items = await _apiClient.DeserializeResponseAsync<List<BizimHesapInvoiceResponse>>(response, ct).ConfigureAwait(false);
            if (items is null) return [];

            return items.Select(r => ErpInvoiceResult.Ok(
                r.InvoiceNumber ?? string.Empty,
                r.Id ?? string.Empty,
                r.InvoiceDate ?? DateTime.MinValue,
                decimal.TryParse(r.GrandTotal, NumberStyles.Any, CultureInfo.InvariantCulture, out var t) ? t : 0m,
                r.PdfUrl)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] GetInvoices exception");
            return [];
        }
    }

    /// <inheritdoc/>
    async Task<bool> IErpInvoiceCapable.CancelInvoiceAsync(string invoiceNumber, string reason, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        _logger.LogInformation("[BizimHesapERPAdapter] Cancelling invoice {InvoiceNumber}, reason: {Reason}", invoiceNumber, reason);

        try
        {
            var payload = new { reason };
            var response = await _apiClient.PutJsonAsync($"api/v1/invoices/{Uri.EscapeDataString(invoiceNumber)}/cancel", payload, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[BizimHesapERPAdapter] Invoice {InvoiceNumber} cancelled", invoiceNumber);
                return true;
            }

            var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
            _logger.LogWarning("[BizimHesapERPAdapter] CancelInvoice failed: {Status} — {Error}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] CancelInvoice exception for {InvoiceNumber}", invoiceNumber);
            return false;
        }
    }

    // ── IErpAccountCapable ─────────────────────────────────────────────

    /// <inheritdoc/>
    async Task<ErpAccountResult> IErpAccountCapable.CreateAccountAsync(ErpAccountRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[BizimHesapERPAdapter] Creating account {AccountCode}", request.AccountCode);

        try
        {
            var payload = new
            {
                name = request.CompanyName,
                taxNumber = request.TaxId,
                taxOffice = request.TaxOffice,
                address = request.Address,
                city = request.City,
                phone = request.Phone,
                email = request.Email
            };

            var response = await _apiClient.PostJsonAsync("api/v1/contacts", payload, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
                _logger.LogWarning("[BizimHesapERPAdapter] CreateAccount failed: {Status} — {Error}", response.StatusCode, errorBody);
                return ErpAccountResult.Failed($"HTTP {(int)response.StatusCode}: {errorBody}");
            }

            var result = await _apiClient.DeserializeResponseAsync<BizimHesapContactResponse>(response, ct).ConfigureAwait(false);
            return ErpAccountResult.Ok(
                result?.Code ?? request.AccountCode,
                result?.Name ?? request.CompanyName,
                0m);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] CreateAccount exception");
            return ErpAccountResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc/>
    async Task<ErpAccountResult?> IErpAccountCapable.GetAccountAsync(string accountCode, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountCode);
        _logger.LogInformation("[BizimHesapERPAdapter] Getting account by tax number {Code}", accountCode);

        try
        {
            var response = await _apiClient.GetAsync($"api/v1/contacts?taxNumber={Uri.EscapeDataString(accountCode)}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
                _logger.LogWarning("[BizimHesapERPAdapter] GetAccount failed: {Status} — {Error}", response.StatusCode, errorBody);
                return null;
            }

            var result = await _apiClient.DeserializeResponseAsync<BizimHesapContactResponse>(response, ct).ConfigureAwait(false);
            if (result is null) return null;

            return ErpAccountResult.Ok(
                result.Code ?? accountCode,
                result.Name ?? string.Empty,
                decimal.TryParse(result.Balance, NumberStyles.Any, CultureInfo.InvariantCulture, out var bal) ? bal : 0m);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] GetAccount exception for {Code}", accountCode);
            return null;
        }
    }

    /// <inheritdoc/>
    async Task<ErpAccountResult> IErpAccountCapable.UpdateAccountAsync(ErpAccountRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[BizimHesapERPAdapter] Updating account {AccountCode}", request.AccountCode);

        try
        {
            var payload = new
            {
                name = request.CompanyName,
                taxNumber = request.TaxId,
                taxOffice = request.TaxOffice,
                address = request.Address,
                city = request.City,
                phone = request.Phone,
                email = request.Email
            };

            var response = await _apiClient.PutJsonAsync($"api/v1/contacts/{Uri.EscapeDataString(request.AccountCode)}", payload, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
                _logger.LogWarning("[BizimHesapERPAdapter] UpdateAccount failed: {Status} — {Error}", response.StatusCode, errorBody);
                return ErpAccountResult.Failed($"HTTP {(int)response.StatusCode}: {errorBody}");
            }

            var result = await _apiClient.DeserializeResponseAsync<BizimHesapContactResponse>(response, ct).ConfigureAwait(false);
            return ErpAccountResult.Ok(
                result?.Code ?? request.AccountCode,
                result?.Name ?? request.CompanyName,
                decimal.TryParse(result?.Balance, NumberStyles.Any, CultureInfo.InvariantCulture, out var bal) ? bal : 0m);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] UpdateAccount exception");
            return ErpAccountResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc/>
    async Task<List<ErpAccountResult>> IErpAccountCapable.SearchAccountsAsync(string query, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        _logger.LogInformation("[BizimHesapERPAdapter] Searching accounts with query '{Query}'", query);

        try
        {
            var response = await _apiClient.GetAsync($"api/v1/contacts?search={Uri.EscapeDataString(query)}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
                _logger.LogWarning("[BizimHesapERPAdapter] SearchAccounts failed: {Status} — {Error}", response.StatusCode, errorBody);
                return [];
            }

            var items = await _apiClient.DeserializeResponseAsync<List<BizimHesapContactResponse>>(response, ct).ConfigureAwait(false);
            if (items is null) return [];

            return items.Select(c => ErpAccountResult.Ok(
                c.Code ?? string.Empty,
                c.Name ?? string.Empty,
                decimal.TryParse(c.Balance, NumberStyles.Any, CultureInfo.InvariantCulture, out var bal) ? bal : 0m)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] SearchAccounts exception");
            return [];
        }
    }

    /// <inheritdoc/>
    async Task<decimal> IErpAccountCapable.GetAccountBalanceAsync(string accountCode, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountCode);
        _logger.LogInformation("[BizimHesapERPAdapter] Getting account balance for {Code}", accountCode);

        try
        {
            var response = await _apiClient.GetAsync($"api/v1/contacts/{Uri.EscapeDataString(accountCode)}/balance", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
                _logger.LogWarning("[BizimHesapERPAdapter] GetAccountBalance failed: {Status} — {Error}", response.StatusCode, errorBody);
                return 0m;
            }

            var result = await _apiClient.DeserializeResponseAsync<BizimHesapContactResponse>(response, ct).ConfigureAwait(false);
            if (result?.Balance is not null &&
                decimal.TryParse(result.Balance, NumberStyles.Any, CultureInfo.InvariantCulture, out var balance))
            {
                return balance;
            }

            return 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] GetAccountBalance exception for {Code}", accountCode);
            return 0m;
        }
    }

    // ── IErpStockCapable ───────────────────────────────────────────────

    /// <inheritdoc/>
    async Task<List<ErpStockItem>> IErpStockCapable.GetStockLevelsAsync(CancellationToken ct)
    {
        _logger.LogInformation("[BizimHesapERPAdapter] Getting all stock levels");

        try
        {
            var response = await _apiClient.GetAsync("api/v1/stock-items", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
                _logger.LogWarning("[BizimHesapERPAdapter] GetStockLevels failed: {Status} — {Error}", response.StatusCode, errorBody);
                return [];
            }

            var items = await _apiClient.DeserializeResponseAsync<List<BizimHesapStockItemResponse>>(response, ct).ConfigureAwait(false);
            if (items is null) return [];

            return items.Select(s => new ErpStockItem(
                s.Code ?? string.Empty,
                s.Name ?? string.Empty,
                s.Quantity,
                s.UnitCode ?? "ADET",
                s.WarehouseCode,
                decimal.TryParse(s.UnitCost, NumberStyles.Any, CultureInfo.InvariantCulture, out var cost) ? cost : null)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] GetStockLevels exception");
            return [];
        }
    }

    /// <inheritdoc/>
    async Task<ErpStockItem?> IErpStockCapable.GetStockByCodeAsync(string productCode, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);
        _logger.LogInformation("[BizimHesapERPAdapter] Getting stock for product {Code}", productCode);

        try
        {
            var response = await _apiClient.GetAsync($"api/v1/stock-items?code={Uri.EscapeDataString(productCode)}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
                _logger.LogWarning("[BizimHesapERPAdapter] GetStockByCode failed: {Status} — {Error}", response.StatusCode, errorBody);
                return null;
            }

            var item = await _apiClient.DeserializeResponseAsync<BizimHesapStockItemResponse>(response, ct).ConfigureAwait(false);
            if (item is null) return null;

            return new ErpStockItem(
                item.Code ?? productCode,
                item.Name ?? string.Empty,
                item.Quantity,
                item.UnitCode ?? "ADET",
                item.WarehouseCode,
                decimal.TryParse(item.UnitCost, NumberStyles.Any, CultureInfo.InvariantCulture, out var cost) ? cost : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] GetStockByCode exception for {Code}", productCode);
            return null;
        }
    }

    /// <inheritdoc/>
    async Task<bool> IErpStockCapable.UpdateStockAsync(string productCode, int quantity, string warehouseCode, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);
        _logger.LogInformation("[BizimHesapERPAdapter] Updating stock {Code} to {Qty} in warehouse {Warehouse}", productCode, quantity, warehouseCode);

        try
        {
            var payload = new { quantity, warehouseCode };
            var response = await _apiClient.PutJsonAsync($"api/v1/stock-items/{Uri.EscapeDataString(productCode)}/quantity", payload, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[BizimHesapERPAdapter] Stock {Code} updated successfully", productCode);
                return true;
            }

            var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
            _logger.LogWarning("[BizimHesapERPAdapter] UpdateStock failed: {Status} — {Error}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] UpdateStock exception for {Code}", productCode);
            return false;
        }
    }

    // ── IErpPriceCapable ──────────────────────────────────────────────

    /// <inheritdoc/>
    async Task<List<ErpPriceItem>> IErpPriceCapable.GetProductPricesAsync(CancellationToken ct)
    {
        _logger.LogInformation("[BizimHesapERPAdapter] Getting all product prices");

        try
        {
            var response = await _apiClient.GetAsync("api/v1/stock-items", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
                _logger.LogWarning("[BizimHesapERPAdapter] GetProductPrices failed: {Status} — {Error}", response.StatusCode, errorBody);
                return [];
            }

            var items = await _apiClient.DeserializeResponseAsync<List<BizimHesapPriceItemResponse>>(response, ct).ConfigureAwait(false);
            if (items is null) return [];

            return items.Select(p => new ErpPriceItem(
                p.Code ?? string.Empty,
                p.Name ?? string.Empty,
                p.PurchasePrice,
                p.SalePrice,
                p.ListPrice,
                p.CurrencyCode ?? "TRY")).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] GetProductPrices exception");
            return [];
        }
    }

    /// <inheritdoc/>
    async Task<ErpPriceItem?> IErpPriceCapable.GetPriceByCodeAsync(string productCode, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);

        try
        {
            var response = await _apiClient.GetAsync($"api/v1/stock-items?code={Uri.EscapeDataString(productCode)}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return null;

            var item = await _apiClient.DeserializeResponseAsync<BizimHesapPriceItemResponse>(response, ct).ConfigureAwait(false);
            if (item is null) return null;

            return new ErpPriceItem(
                item.Code ?? productCode,
                item.Name ?? string.Empty,
                item.PurchasePrice,
                item.SalePrice,
                item.ListPrice,
                item.CurrencyCode ?? "TRY");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] GetPriceByCode exception for {Code}", productCode);
            return null;
        }
    }

    // ── IErpWaybillCapable ────────────────────────────────────────────

    /// <inheritdoc/>
    async Task<ErpWaybillResult> IErpWaybillCapable.CreateWaybillAsync(ErpWaybillRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[BizimHesapERPAdapter] CreateWaybill — Customer:{Customer}", request.CustomerCode);

        try
        {
            var payload = new
            {
                customerCode = request.CustomerCode,
                shippingAddress = request.ShippingAddress,
                cargoFirm = request.CargoFirm,
                trackingNumber = request.TrackingNumber,
                date = DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                lines = request.Lines.Select(l => new
                {
                    productCode = l.ProductCode,
                    quantity = l.Quantity,
                    unitCode = l.UnitCode
                }).ToArray()
            };

            var response = await _apiClient.PostJsonAsync("api/v1/waybills", payload, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await _apiClient.DeserializeResponseAsync<BizimHesapWaybillResponse>(response, ct).ConfigureAwait(false);
                var number = result?.WaybillNumber ?? string.Empty;
                _logger.LogInformation("[BizimHesapERPAdapter] Waybill created — Number:{Number}", number);
                return ErpWaybillResult.Ok(number, DateTime.Today);
            }

            var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
            _logger.LogWarning("[BizimHesapERPAdapter] CreateWaybill failed: {Status} — {Error}", response.StatusCode, errorBody);
            return ErpWaybillResult.Failed($"HTTP {(int)response.StatusCode}: {errorBody[..Math.Min(100, errorBody.Length)]}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] CreateWaybill exception");
            return ErpWaybillResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc/>
    async Task<ErpWaybillResult?> IErpWaybillCapable.GetWaybillAsync(string waybillNumber, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(waybillNumber);

        try
        {
            var response = await _apiClient.GetAsync($"api/v1/waybills/{Uri.EscapeDataString(waybillNumber)}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return null;

            var result = await _apiClient.DeserializeResponseAsync<BizimHesapWaybillResponse>(response, ct).ConfigureAwait(false);
            if (result is null) return null;

            return ErpWaybillResult.Ok(
                result.WaybillNumber ?? waybillNumber,
                result.WaybillDate ?? DateTime.Today);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] GetWaybill exception — Number:{Number}", waybillNumber);
            return null;
        }
    }

    // ── IErpBankCapable ───────────────────────────────────────────────

    /// <inheritdoc/>
    async Task<List<ErpBankTransaction>> IErpBankCapable.GetTransactionsAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        _logger.LogInformation("[BizimHesapERPAdapter] GetTransactions — From:{From} To:{To}", from, to);

        try
        {
            var fromStr = from.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var toStr = to.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var response = await _apiClient.GetAsync($"api/v1/bank-transactions?from={fromStr}&to={toStr}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
                _logger.LogWarning("[BizimHesapERPAdapter] GetTransactions failed: {Status} — {Error}", response.StatusCode, errorBody);
                return [];
            }

            var items = await _apiClient.DeserializeResponseAsync<List<BizimHesapBankTransactionResponse>>(response, ct).ConfigureAwait(false);
            if (items is null) return [];

            return items.Select(t => new ErpBankTransaction(
                t.TransactionDate,
                t.Amount,
                t.Description ?? string.Empty,
                t.TransactionType ?? "OTHER",
                t.Reference)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] GetTransactions exception");
            return [];
        }
    }

    /// <inheritdoc/>
    async Task<ErpPaymentResult> IErpBankCapable.RecordPaymentAsync(ErpPaymentRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[BizimHesapERPAdapter] RecordPayment — Account:{Account} Amount:{Amount}",
            request.AccountCode, request.Amount);

        try
        {
            var payload = new
            {
                accountCode = request.AccountCode,
                amount = request.Amount,
                paymentType = request.PaymentType,
                dueDate = request.DueDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                description = request.Description
            };

            var response = await _apiClient.PostJsonAsync("api/v1/payments", payload, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await _apiClient.DeserializeResponseAsync<BizimHesapPaymentResponse>(response, ct).ConfigureAwait(false);
                return ErpPaymentResult.Ok(result?.Reference ?? string.Empty);
            }

            var errorBody = await BizimHesapApiClient.ReadErrorBodyAsync(response, ct).ConfigureAwait(false);
            return ErpPaymentResult.Failed($"HTTP {(int)response.StatusCode}: {errorBody[..Math.Min(100, errorBody.Length)]}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BizimHesapERPAdapter] RecordPayment exception");
            return ErpPaymentResult.Failed(ex.Message);
        }
    }
}
