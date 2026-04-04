using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Infrastructure.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using InvoiceEntity = MesTech.Domain.Entities.Invoice;

namespace MesTech.Infrastructure.Integration.ERP.ERPNext;

/// <summary>
/// ERPNext REST adapter — Frappe API integration.
/// Auth: token {api_key}:{api_secret} (Authorization header).
/// Endpoints: POST /api/resource/{DocType} — creates documents.
///            GET /api/resource/{DocType}/{name} — reads documents.
///            GET /api/method/erpnext.accounts.utils.get_balance_on — GL balance.
/// Implements IERPAdapter for compatibility with ERPAdapterFactory.
/// Also implements IErpAdapter (Dalga 11 modern interface) for unified ERP access.
/// </summary>
public sealed class ERPNextRestAdapter : IERPAdapter, MesTech.Application.Interfaces.Erp.IErpAdapter, MesTech.Application.Interfaces.IErpBridgeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ERPNextRestAdapter> _logger;
    private readonly ERPNextOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public string ERPName => "ERPNext";
    public MesTech.Domain.Enums.ErpProvider Provider => MesTech.Domain.Enums.ErpProvider.ERPNext;

    public ERPNextRestAdapter(
        HttpClient httpClient,
        ILogger<ERPNextRestAdapter> logger,
        IOptions<ERPNextOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (_options.IsConfigured)
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("token", $"{_options.ApiKey}:{_options.ApiSecret}");
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            using var response = await _httpClient.GetAsync("api/method/frappe.auth.get_logged_user", ct)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("[ERPNext] Connection test OK — user: {Body}", body[..Math.Min(body.Length, 100)]);
                return true;
            }

            _logger.LogWarning("[ERPNext] Connection test failed: {Status}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERPNext] Connection test error");
            return false;
        }
    }

    public async Task SyncInvoicesAsync(IReadOnlyList<InvoiceEntity> invoices, CancellationToken ct = default)
    {
        EnsureConfigured();

        foreach (var invoice in invoices)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var payload = new
                {
                    doctype = "Sales Invoice",
                    company = _options.Company,
                    customer = invoice.CustomerName ?? "Walk-in Customer",
                    posting_date = invoice.CreatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    due_date = invoice.CreatedAt.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    currency = "TRY",
                    items = invoice.Lines.Select(item => new
                    {
                        item_code = item.SKU ?? item.ProductName ?? "MISC",
                        description = item.ProductName,
                        qty = item.Quantity,
                        rate = item.UnitPrice,
                        amount = item.LineTotal
                    }).ToArray(),
                    custom_mestech_invoice_id = invoice.Id.ToString()
                };

                var result = await PostResourceAsync("Sales Invoice", payload, ct).ConfigureAwait(false);

                _logger.LogInformation(
                    "[ERPNext] Sales Invoice created: {InvoiceId} → ERPNext ref: {Ref}",
                    invoice.Id, result);

                ErpMetrics.SyncTotal.WithLabels("erpnext", "invoice", "success").Inc();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERPNext] Failed to sync invoice {InvoiceId}", invoice.Id);
                ErpMetrics.SyncTotal.WithLabels("erpnext", "invoice", "error").Inc();
            }
        }
    }

    public async Task SyncExpensesAsync(IReadOnlyList<AccountingExpenseDto> expenses, CancellationToken ct = default)
    {
        EnsureConfigured();

        foreach (var expense in expenses)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var payload = new
                {
                    doctype = "Purchase Invoice",
                    company = _options.Company,
                    supplier = expense.SupplierName ?? "Unknown Supplier",
                    posting_date = expense.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    currency = "TRY",
                    items = new[]
                    {
                        new
                        {
                            item_code = expense.Category ?? "Expense",
                            description = expense.Description,
                            qty = 1,
                            rate = expense.Amount,
                            expense_account = expense.GlAccountCode ?? "5100 - Cost of Goods Sold - MT"
                        }
                    }
                };

                await PostResourceAsync("Purchase Invoice", payload, ct).ConfigureAwait(false);

                _logger.LogInformation("[ERPNext] Purchase Invoice created for expense: {ExpenseId}", expense.Id);
                ErpMetrics.SyncTotal.WithLabels("erpnext", "expense", "success").Inc();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERPNext] Failed to sync expense {ExpenseId}", expense.Id);
                ErpMetrics.SyncTotal.WithLabels("erpnext", "expense", "error").Inc();
            }
        }
    }

    public async Task SyncCounterpartiesAsync(IReadOnlyList<CounterpartyDto> parties, CancellationToken ct = default)
    {
        EnsureConfigured();

        foreach (var party in parties)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var doctype = party.IsSupplier ? "Supplier" : "Customer";
                var payload = new
                {
                    doctype,
                    customer_name = party.Name,
                    supplier_name = party.IsSupplier ? party.Name : (string?)null,
                    customer_type = party.IsCompany ? "Company" : "Individual",
                    tax_id = party.TaxId,
                    customer_group = "All Customer Groups",
                    territory = "Turkey"
                };

                await PostResourceAsync(doctype, payload, ct).ConfigureAwait(false);

                _logger.LogInformation("[ERPNext] {DocType} synced: {Name}", doctype, party.Name);
                ErpMetrics.SyncTotal.WithLabels("erpnext", "counterparty", "success").Inc();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERPNext] Failed to sync counterparty {Name}", party.Name);
                ErpMetrics.SyncTotal.WithLabels("erpnext", "counterparty", "error").Inc();
            }
        }
    }

    public async Task<decimal> GetBalanceAsync(string accountCode, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            var url = $"api/method/erpnext.accounts.utils.get_balance_on?account={Uri.EscapeDataString(accountCode)}" +
                      $"&date={DateTime.UtcNow:yyyy-MM-dd}&company={Uri.EscapeDataString(_options.Company)}";

            using var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);

            if (doc.RootElement.TryGetProperty("message", out var msg) && msg.TryGetDecimal(out var balance))
                return balance;

            _logger.LogWarning("[ERPNext] GetBalance response missing 'message' field for account {Account}", accountCode);
            return 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERPNext] GetBalance failed for account {Account}", accountCode);
            return 0m;
        }
    }

    // ── IErpBridgeService — Event-driven push methods ─────────────────────

    public async Task PushSalesInvoiceAsync(Guid tenantId, Guid orderId, string orderNumber,
        decimal totalAmount, string? customerName, CancellationToken ct = default)
    {
        EnsureConfigured();
        var payload = new
        {
            doctype = "Sales Invoice",
            company = _options.Company,
            customer = customerName ?? "Walk-in Customer",
            posting_date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            currency = "TRY",
            items = new[] { new { item_code = orderNumber, description = $"Order {orderNumber}", qty = 1, rate = totalAmount } },
            custom_mestech_order_id = orderId.ToString()
        };
        await PostResourceAsync("Sales Invoice", payload, ct).ConfigureAwait(false);
        _logger.LogInformation("[ERPNext] PushSalesInvoice: order={OrderNumber} amount={Amount}", orderNumber, totalAmount);
    }

    public async Task PushStockEntryAsync(Guid tenantId, Guid productId, string sku,
        string entryType, int quantity, string reason, CancellationToken ct = default)
    {
        EnsureConfigured();
        var purposeMap = entryType switch
        {
            "Receipt" or "receipt" => "Material Receipt",
            "Issue" or "issue" => "Material Issue",
            _ => "Material Transfer"
        };
        var payload = new
        {
            doctype = "Stock Entry",
            company = _options.Company,
            stock_entry_type = purposeMap,
            items = new[] { new { item_code = sku, qty = Math.Abs(quantity), t_warehouse = _options.DefaultWarehouse } },
            custom_reason = reason
        };
        await PostResourceAsync("Stock Entry", payload, ct).ConfigureAwait(false);
        _logger.LogInformation("[ERPNext] PushStockEntry: sku={Sku} type={Type} qty={Qty}", sku, purposeMap, quantity);
    }

    public async Task PushCustomerAsync(Guid tenantId, Guid customerId, string customerName,
        string? email, string? phone, CancellationToken ct = default)
    {
        EnsureConfigured();
        var payload = new
        {
            doctype = "Customer",
            customer_name = customerName,
            customer_type = "Individual",
            customer_group = "All Customer Groups",
            territory = "Turkey",
            email_id = email,
            mobile_no = phone
        };
        await PostResourceAsync("Customer", payload, ct).ConfigureAwait(false);
        _logger.LogInformation("[ERPNext] PushCustomer: name={Name}", customerName);
    }

    // ── Frappe REST helpers ─────────────────────────────────────────────────

    private async Task<string?> PostResourceAsync(string doctype, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(new { data = payload }, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync($"api/resource/{doctype}", content, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError("[ERPNext] POST /api/resource/{DocType} failed: {Status} — {Error}",
                doctype, response.StatusCode, errorBody[..Math.Min(errorBody.Length, 500)]);
            throw new HttpRequestException($"ERPNext API error: {response.StatusCode} — {errorBody[..Math.Min(errorBody.Length, 200)]}");
        }

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(body);

        return doc.RootElement.TryGetProperty("data", out var data) &&
               data.TryGetProperty("name", out var name)
            ? name.GetString()
            : null;
    }

    private void EnsureConfigured()
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("ERPNext is not configured. Set ERP:ERPNext section in appsettings.json.");
    }

    // ═══ IErpAdapter (Dalga 11) ═══

    public async Task<MesTech.Application.DTOs.ERP.ErpSyncResult> SyncOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("[ERPNext] SyncOrderAsync: {OrderId}", orderId);

        var docName = await PostResourceAsync("Sales Order", new
        {
            customer = "MesTech-Default",
            transaction_date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            custom_mestech_order_id = orderId.ToString()
        }, ct).ConfigureAwait(false);

        return docName is not null
            ? MesTech.Application.DTOs.ERP.ErpSyncResult.Ok(docName)
            : MesTech.Application.DTOs.ERP.ErpSyncResult.Fail("ERPNext Sales Order creation failed");
    }

    public async Task<MesTech.Application.DTOs.ERP.ErpSyncResult> SyncInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("[ERPNext] SyncInvoiceAsync: {InvoiceId}", invoiceId);

        var docName = await PostResourceAsync("Sales Invoice", new
        {
            customer = "MesTech-Default",
            posting_date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            custom_mestech_invoice_id = invoiceId.ToString()
        }, ct).ConfigureAwait(false);

        return docName is not null
            ? MesTech.Application.DTOs.ERP.ErpSyncResult.Ok(docName)
            : MesTech.Application.DTOs.ERP.ErpSyncResult.Fail("ERPNext Sales Invoice creation failed");
    }

    public async Task<IReadOnlyList<MesTech.Application.DTOs.ERP.ErpAccountDto>> GetAccountBalancesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        var balance = await GetBalanceAsync("1200 - Debtors - MES", ct).ConfigureAwait(false);
        return new[]
        {
            new MesTech.Application.DTOs.ERP.ErpAccountDto("1200", "Debtors", balance, "TRY")
        };
    }

    public Task<bool> PingAsync(CancellationToken ct = default) => TestConnectionAsync(ct);
}
