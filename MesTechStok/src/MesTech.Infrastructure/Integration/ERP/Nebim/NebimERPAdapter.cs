using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace MesTech.Infrastructure.Integration.ERP.Nebim;

/// <summary>
/// Nebim V3 ERP adapter — Nebim V3 REST API.
/// Auth: API Key header ("X-Nebim-ApiKey").
/// Implements IErpAdapter (Dalga 11 enum-based interface) + IErpStockCapable + IErpInvoiceCapable + IErpAccountCapable.
///
/// Config keys (via NebimOptions):
///   - ERP:Nebim:BaseUrl       (e.g. "https://api.nebim.com/v3")
///   - ERP:Nebim:ApiKey
///   - ERP:Nebim:DatabaseCode
///   - ERP:Nebim:OfficeCode
///   - ERP:Nebim:WarehouseCode
///
/// Nebim V3 API endpoints:
///   - GET  /api/products          (product catalog)
///   - GET  /api/inventory/levels  (stock levels)
///   - POST /api/invoices          (invoice sync)
///   - GET  /api/customers         (account balances)
///   - GET  /api/ping              (health check)
/// </summary>
public sealed class NebimERPAdapter : IErpAdapter, IErpStockCapable, IErpInvoiceCapable, IErpAccountCapable, IErpWaybillCapable, IErpPriceCapable, IErpBankCapable
{
    private readonly HttpClient _httpClient;
    private readonly NebimOptions _options;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<NebimERPAdapter> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ErpProvider Provider => ErpProvider.Nebim;

    public NebimERPAdapter(
        HttpClient httpClient,
        IOptions<NebimOptions> options,
        IOrderRepository orderRepository,
        ILogger<NebimERPAdapter> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private string BaseUrl => !string.IsNullOrWhiteSpace(_options.BaseUrl)
        ? _options.BaseUrl.TrimEnd('/')
        : throw new InvalidOperationException("ERP:Nebim:BaseUrl is not configured.");

    // ═══════════════════════════════════════════════════════════════════
    // IErpAdapter — Dalga 15: Nebim V3 ERP integration
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Syncs a MesTech Order to Nebim as a sales order.
    /// Fetches Order by ID, maps to Nebim format, POSTs to /api/invoices (sales type).
    /// </summary>
    public async Task<ErpSyncResult> SyncOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        if (orderId == Guid.Empty)
            return ErpSyncResult.Fail("OrderId cannot be empty.");

        _logger.LogInformation(
            "[NebimERPAdapter] SyncOrderAsync — OrderId:{OrderId}", orderId);

#pragma warning disable CA1031 // Intentional: ERP sync failure must be returned, not propagated
        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId, ct).ConfigureAwait(false);
            if (order is null)
            {
                _logger.LogWarning(
                    "[NebimERPAdapter] Order not found: {OrderId}", orderId);
                return ErpSyncResult.Fail($"Order not found: {orderId}");
            }

            SetApiKeyHeader();

            var firstItem = order.OrderItems.FirstOrDefault();
            var nebimOrder = new
            {
                officeCode = _options.OfficeCode,
                warehouseCode = _options.WarehouseCode,
                documentNumber = order.OrderNumber,
                documentDate = order.OrderDate.ToString("yyyy-MM-dd"),
                currAccCode = "ETICARET",
                lines = new[]
                {
                    new
                    {
                        productCode = firstItem?.ProductSKU ?? "GENEL",
                        qty = firstItem?.Quantity ?? 1,
                        price = (double)order.TotalAmount,
                        vatRate = 20
                    }
                },
                description = $"MesTech Siparis #{order.OrderNumber}"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(nebimOrder, JsonOptions),
                Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(
                $"{BaseUrl}/api/invoices", content, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var json = JsonDocument.Parse(body);
                var erpRef = json.RootElement.TryGetProperty("invoiceNumber", out var inv)
                    ? inv.GetString() ?? "OK" : "OK";

                _logger.LogInformation(
                    "[NebimERPAdapter] Order synced — OrderId:{OrderId} NebimRef:{ErpRef}",
                    orderId, erpRef);
                return ErpSyncResult.Ok(erpRef);
            }

            var err = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning(
                "[NebimERPAdapter] Order sync failed — OrderId:{OrderId} HTTP {Status}: {Error}",
                orderId, (int)response.StatusCode, err[..Math.Min(200, err.Length)]);
            return ErpSyncResult.Fail(
                $"HTTP {(int)response.StatusCode}: {err[..Math.Min(100, err.Length)]}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[NebimERPAdapter] SyncOrderAsync exception — OrderId:{OrderId}", orderId);
            return ErpSyncResult.Fail(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Syncs a MesTech Invoice to Nebim.
    /// POSTs to /api/invoices.
    /// </summary>
    public async Task<ErpSyncResult> SyncInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (invoiceId == Guid.Empty)
            return ErpSyncResult.Fail("InvoiceId cannot be empty.");

        _logger.LogInformation(
            "[NebimERPAdapter] SyncInvoiceAsync — InvoiceId:{InvoiceId}", invoiceId);

#pragma warning disable CA1031 // Intentional: ERP sync failure must be returned, not propagated
        try
        {
            SetApiKeyHeader();

            var nebimInvoice = new
            {
                officeCode = _options.OfficeCode,
                warehouseCode = _options.WarehouseCode,
                invoiceNumber = invoiceId.ToString("N")[..16],
                invoiceDate = DateTime.Today.ToString("yyyy-MM-dd"),
                invoiceType = "Sales"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(nebimInvoice, JsonOptions),
                Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(
                $"{BaseUrl}/api/invoices", content, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var erpRef = $"NEBIM-INV-{invoiceId:N}"[..32];
                _logger.LogInformation(
                    "[NebimERPAdapter] Invoice synced — InvoiceId:{InvoiceId} NebimRef:{ErpRef}",
                    invoiceId, erpRef);
                return ErpSyncResult.Ok(erpRef);
            }

            var err = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning(
                "[NebimERPAdapter] Invoice sync failed — InvoiceId:{InvoiceId} HTTP {Status}: {Error}",
                invoiceId, (int)response.StatusCode, err[..Math.Min(200, err.Length)]);
            return ErpSyncResult.Fail($"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[NebimERPAdapter] SyncInvoiceAsync exception — InvoiceId:{InvoiceId}", invoiceId);
            return ErpSyncResult.Fail(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Retrieves account balances from Nebim.
    /// GET /api/customers — parses JSON array of customer records.
    /// </summary>
    public async Task<IReadOnlyList<ErpAccountDto>> GetAccountBalancesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[NebimERPAdapter] GetAccountBalancesAsync");

#pragma warning disable CA1031 // Intentional: graceful degradation — return empty on error
        try
        {
            SetApiKeyHeader();

            using var response = await _httpClient.GetAsync(
                $"{BaseUrl}/api/customers?officeCode={_options.OfficeCode}&limit=200", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning(
                    "[NebimERPAdapter] GetAccountBalances failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return Array.Empty<ErpAccountDto>();
            }

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var accounts = new List<ErpAccountDto>();

            foreach (var item in json.RootElement.EnumerateArray())
            {
                var accountCode = item.TryGetProperty("currAccCode", out var cc)
                    ? cc.GetString() ?? string.Empty : string.Empty;
                var accountName = item.TryGetProperty("currAccDescription", out var cd)
                    ? cd.GetString() ?? string.Empty : string.Empty;
                var balance = item.TryGetProperty("balance", out var b)
                    ? b.GetDecimal() : 0m;
                var currency = item.TryGetProperty("currencyCode", out var cur)
                    ? cur.GetString() ?? "TRY" : "TRY";

                accounts.Add(new ErpAccountDto(accountCode, accountName, balance, currency));
            }

            _logger.LogInformation(
                "[NebimERPAdapter] Retrieved {Count} account balances from Nebim",
                accounts.Count);

            return accounts.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] GetAccountBalancesAsync exception");
            return Array.Empty<ErpAccountDto>();
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Health check for Nebim V3 REST API.
    /// GET /api/ping — returns true if server responds with 2xx.
    /// </summary>
    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
#pragma warning disable CA1031 // Intentional: health check must not throw
        try
        {
            SetApiKeyHeader();
            using var response = await _httpClient.GetAsync($"{BaseUrl}/api/ping", ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[NebimERPAdapter] Ping OK");
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("[NebimERPAdapter] Ping failed: {Status} — {Error}",
                response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] Ping exception");
            return false;
        }
#pragma warning restore CA1031
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpStockCapable — Stock level queries and updates
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retrieves all stock levels from Nebim inventory.
    /// GET /api/inventory/levels
    /// </summary>
    public async Task<List<ErpStockItem>> GetStockLevelsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[NebimERPAdapter] GetStockLevelsAsync");

#pragma warning disable CA1031 // Intentional: graceful degradation — return empty on error
        try
        {
            SetApiKeyHeader();

            var url = $"{BaseUrl}/api/inventory/levels?warehouseCode={_options.WarehouseCode}&officeCode={_options.OfficeCode}";
            using var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning(
                    "[NebimERPAdapter] GetStockLevels failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return new List<ErpStockItem>();
            }

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var items = new List<ErpStockItem>();

            foreach (var item in json.RootElement.EnumerateArray())
            {
                var productCode = item.TryGetProperty("productCode", out var pc)
                    ? pc.GetString() ?? string.Empty : string.Empty;
                var qty = item.TryGetProperty("availableQty", out var q)
                    ? q.GetInt32() : 0;
                var wh = item.TryGetProperty("warehouseCode", out var w)
                    ? w.GetString() : _options.WarehouseCode;

                items.Add(new ErpStockItem(productCode, productCode, qty, "AD", wh, null));
            }

            _logger.LogInformation(
                "[NebimERPAdapter] Retrieved {Count} stock items from Nebim", items.Count);
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] GetStockLevelsAsync exception");
            return new List<ErpStockItem>();
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Retrieves stock level for a specific product code.
    /// GET /api/inventory/levels?productCode={productCode}
    /// </summary>
    public async Task<ErpStockItem?> GetStockByCodeAsync(string productCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return null;

        _logger.LogInformation(
            "[NebimERPAdapter] GetStockByCodeAsync — ProductCode:{ProductCode}", productCode);

#pragma warning disable CA1031 // Intentional: graceful degradation — return null on error
        try
        {
            SetApiKeyHeader();

            var url = $"{BaseUrl}/api/inventory/levels?productCode={productCode}&warehouseCode={_options.WarehouseCode}";
            using var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning(
                    "[NebimERPAdapter] GetStockByCode failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return null;
            }

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var first = json.RootElement.EnumerateArray().FirstOrDefault();

            if (first.ValueKind == JsonValueKind.Undefined)
                return null;

            var qty = first.TryGetProperty("availableQty", out var q) ? q.GetInt32() : 0;
            var wh = first.TryGetProperty("warehouseCode", out var w)
                ? w.GetString() : _options.WarehouseCode;

            return new ErpStockItem(productCode, productCode, qty, "AD", wh, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[NebimERPAdapter] GetStockByCodeAsync exception — ProductCode:{ProductCode}", productCode);
            return null;
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Updates stock quantity for a product in Nebim.
    /// POST /api/inventory/levels (upsert).
    /// </summary>
    public async Task<bool> UpdateStockAsync(string productCode, int quantity, string warehouseCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return false;

        _logger.LogInformation(
            "[NebimERPAdapter] UpdateStockAsync — ProductCode:{ProductCode} Qty:{Quantity} WH:{Warehouse}",
            productCode, quantity, warehouseCode);

#pragma warning disable CA1031 // Intentional: stock update failure must be returned, not propagated
        try
        {
            SetApiKeyHeader();

            var payload = new
            {
                officeCode = _options.OfficeCode,
                warehouseCode = !string.IsNullOrWhiteSpace(warehouseCode) ? warehouseCode : _options.WarehouseCode,
                productCode,
                qty = quantity
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(
                $"{BaseUrl}/api/inventory/levels", content, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "[NebimERPAdapter] Stock updated — ProductCode:{ProductCode} Qty:{Quantity}",
                    productCode, quantity);
                return true;
            }

            var err = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning(
                "[NebimERPAdapter] Stock update failed — ProductCode:{ProductCode} HTTP {Status}: {Error}",
                productCode, (int)response.StatusCode, err[..Math.Min(200, err.Length)]);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[NebimERPAdapter] UpdateStockAsync exception — ProductCode:{ProductCode}", productCode);
            return false;
        }
#pragma warning restore CA1031
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpInvoiceCapable — Invoice CRUD via Nebim V3 API
    // ═══════════════════════════════════════════════════════════════════

    public async Task<ErpInvoiceResult> CreateInvoiceAsync(ErpInvoiceRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[NebimERPAdapter] CreateInvoiceAsync — Customer:{Customer}", request.CustomerCode);

#pragma warning disable CA1031 // Intentional: ERP invoice failure must be returned, not propagated
        try
        {
            SetApiKeyHeader();

            var nebimInvoice = new
            {
                officeCode = _options.OfficeCode,
                warehouseCode = _options.WarehouseCode,
                currAccCode = request.CustomerCode,
                invoiceDate = DateTime.Today.ToString("yyyy-MM-dd"),
                invoiceType = "Sales",
                lines = request.Lines.Select(l => new
                {
                    productCode = l.ProductCode,
                    productName = l.ProductName,
                    qty = l.Quantity,
                    price = (double)l.UnitPrice,
                    vatRate = l.TaxRate,
                    discountAmount = (double)(l.DiscountAmount ?? 0m)
                }).ToArray(),
                description = request.Notes,
                currency = request.Currency,
                subTotal = (double)request.SubTotal,
                taxTotal = (double)request.TaxTotal,
                grandTotal = (double)request.GrandTotal
            };

            var content = new StringContent(
                JsonSerializer.Serialize(nebimInvoice, JsonOptions),
                Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync($"{BaseUrl}/api/invoices", content, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var json = JsonDocument.Parse(body);
                var invoiceNumber = json.RootElement.TryGetProperty("invoiceNumber", out var inv)
                    ? inv.GetString() ?? string.Empty : $"NEBIM-{DateTime.UtcNow:yyyyMMddHHmmss}";
                var erpRef = json.RootElement.TryGetProperty("erpRef", out var er)
                    ? er.GetString() ?? invoiceNumber : invoiceNumber;
                var totalAmount = json.RootElement.TryGetProperty("grandTotal", out var ta)
                    ? ta.GetDecimal() : request.GrandTotal;
                var pdfUrl = json.RootElement.TryGetProperty("pdfUrl", out var pu)
                    ? pu.GetString() : null;

                _logger.LogInformation("[NebimERPAdapter] Invoice created — Number:{Number}", invoiceNumber);
                return ErpInvoiceResult.Ok(invoiceNumber, erpRef, DateTime.Today, totalAmount, pdfUrl);
            }

            var err = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("[NebimERPAdapter] CreateInvoice failed: HTTP {Status}: {Error}",
                (int)response.StatusCode, err[..Math.Min(200, err.Length)]);
            return ErpInvoiceResult.Failed($"HTTP {(int)response.StatusCode}: {err[..Math.Min(100, err.Length)]}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] CreateInvoiceAsync exception");
            return ErpInvoiceResult.Failed(ex.Message);
        }
#pragma warning restore CA1031
    }

    public async Task<ErpInvoiceResult?> GetInvoiceAsync(string invoiceNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber)) return null;

#pragma warning disable CA1031 // Intentional: graceful degradation — return null on error
        try
        {
            SetApiKeyHeader();
            using var response = await _httpClient.GetAsync(
                $"{BaseUrl}/api/invoices/{Uri.EscapeDataString(invoiceNumber)}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("[NebimERPAdapter] GetInvoice failed: {Status} — {Error}",
                    (int)response.StatusCode, errorBody);
                return null;
            }

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var date = json.RootElement.TryGetProperty("invoiceDate", out var d)
                ? DateTime.TryParse(d.GetString(), out var dt) ? dt : DateTime.Today : DateTime.Today;
            var amount = json.RootElement.TryGetProperty("grandTotal", out var a) ? a.GetDecimal() : 0m;
            var erpRef = json.RootElement.TryGetProperty("erpRef", out var er)
                ? er.GetString() ?? invoiceNumber : invoiceNumber;
            var pdfUrl = json.RootElement.TryGetProperty("pdfUrl", out var pu)
                ? pu.GetString() : null;

            return ErpInvoiceResult.Ok(invoiceNumber, erpRef, date, amount, pdfUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] GetInvoiceAsync exception — {Number}", invoiceNumber);
            return null;
        }
#pragma warning restore CA1031
    }

    public async Task<List<ErpInvoiceResult>> GetInvoicesAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
#pragma warning disable CA1031 // Intentional: graceful degradation — return empty on error
        try
        {
            SetApiKeyHeader();
            var url = $"{BaseUrl}/api/invoices?fromDate={from:yyyy-MM-dd}&toDate={to:yyyy-MM-dd}&officeCode={_options.OfficeCode}";
            using var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("[NebimERPAdapter] GetInvoices failed: {Status} — {Error}",
                    (int)response.StatusCode, errorBody);
                return new List<ErpInvoiceResult>();
            }

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var results = new List<ErpInvoiceResult>();

            foreach (var item in json.RootElement.EnumerateArray())
            {
                var number = item.TryGetProperty("invoiceNumber", out var n) ? n.GetString() ?? "" : "";
                var erpRef = item.TryGetProperty("erpRef", out var er) ? er.GetString() ?? number : number;
                var date = item.TryGetProperty("invoiceDate", out var d)
                    ? DateTime.TryParse(d.GetString(), out var dt) ? dt : DateTime.Today : DateTime.Today;
                var amount = item.TryGetProperty("grandTotal", out var a) ? a.GetDecimal() : 0m;
                var pdfUrl = item.TryGetProperty("pdfUrl", out var pu) ? pu.GetString() : null;

                results.Add(ErpInvoiceResult.Ok(number, erpRef, date, amount, pdfUrl));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] GetInvoicesAsync exception");
            return new List<ErpInvoiceResult>();
        }
#pragma warning restore CA1031
    }

    public async Task<bool> CancelInvoiceAsync(string invoiceNumber, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber)) return false;

#pragma warning disable CA1031 // Intentional: ERP cancel failure must be returned, not propagated
        try
        {
            SetApiKeyHeader();
            var payload = new { invoiceNumber, reason, status = "Cancelled" };
            var content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Put,
                $"{BaseUrl}/api/invoices/{Uri.EscapeDataString(invoiceNumber)}/cancel")
            { Content = content };

            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("[NebimERPAdapter] CancelInvoice failed: {Status} — {Error}",
                    (int)response.StatusCode, errorBody);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] CancelInvoiceAsync exception — {Number}", invoiceNumber);
            return false;
        }
#pragma warning restore CA1031
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpAccountCapable — Account CRUD via Nebim V3 API
    // ═══════════════════════════════════════════════════════════════════

    public async Task<ErpAccountResult> CreateAccountAsync(ErpAccountRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[NebimERPAdapter] CreateAccountAsync — Code:{Code}", request.AccountCode);

#pragma warning disable CA1031 // Intentional: ERP account failure must be returned, not propagated
        try
        {
            SetApiKeyHeader();
            var nebimAccount = new
            {
                currAccCode = request.AccountCode,
                currAccDescription = request.CompanyName,
                taxNumber = request.TaxId,
                taxOffice = request.TaxOffice,
                address = request.Address,
                city = request.City,
                phone = request.Phone,
                email = request.Email,
                officeCode = _options.OfficeCode
            };

            var content = new StringContent(
                JsonSerializer.Serialize(nebimAccount, JsonOptions), Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync($"{BaseUrl}/api/customers", content, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[NebimERPAdapter] Account created — Code:{Code}", request.AccountCode);
                return ErpAccountResult.Ok(request.AccountCode, request.CompanyName, 0m);
            }

            var err = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return ErpAccountResult.Failed($"HTTP {(int)response.StatusCode}: {err[..Math.Min(100, err.Length)]}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] CreateAccountAsync exception");
            return ErpAccountResult.Failed(ex.Message);
        }
#pragma warning restore CA1031
    }

    public async Task<ErpAccountResult?> GetAccountAsync(string accountCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accountCode)) return null;

#pragma warning disable CA1031 // Intentional: graceful degradation — return null on error
        try
        {
            SetApiKeyHeader();
            using var response = await _httpClient.GetAsync(
                $"{BaseUrl}/api/customers/{Uri.EscapeDataString(accountCode)}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("[NebimERPAdapter] GetAccount failed: {Status} — {Error}",
                    (int)response.StatusCode, errorBody);
                return null;
            }

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var name = json.RootElement.TryGetProperty("currAccDescription", out var n)
                ? n.GetString() ?? "" : "";
            var balance = json.RootElement.TryGetProperty("balance", out var b)
                ? b.GetDecimal() : 0m;
            var currency = json.RootElement.TryGetProperty("currencyCode", out var c)
                ? c.GetString() ?? "TRY" : "TRY";

            return ErpAccountResult.Ok(accountCode, name, balance, currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] GetAccountAsync exception — {Code}", accountCode);
            return null;
        }
#pragma warning restore CA1031
    }

    public async Task<ErpAccountResult> UpdateAccountAsync(ErpAccountRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

#pragma warning disable CA1031 // Intentional: ERP update failure must be returned, not propagated
        try
        {
            SetApiKeyHeader();
            var nebimAccount = new
            {
                currAccDescription = request.CompanyName,
                taxNumber = request.TaxId,
                taxOffice = request.TaxOffice,
                address = request.Address,
                city = request.City,
                phone = request.Phone,
                email = request.Email
            };

            var content = new StringContent(
                JsonSerializer.Serialize(nebimAccount, JsonOptions), Encoding.UTF8, "application/json");
            var httpRequest = new HttpRequestMessage(HttpMethod.Put,
                $"{BaseUrl}/api/customers/{Uri.EscapeDataString(request.AccountCode)}")
            { Content = content };

            using var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return ErpAccountResult.Ok(request.AccountCode, request.CompanyName, 0m);

            var err = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return ErpAccountResult.Failed($"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] UpdateAccountAsync exception");
            return ErpAccountResult.Failed(ex.Message);
        }
#pragma warning restore CA1031
    }

    public async Task<List<ErpAccountResult>> SearchAccountsAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<ErpAccountResult>();

#pragma warning disable CA1031 // Intentional: graceful degradation — return empty on error
        try
        {
            SetApiKeyHeader();
            using var response = await _httpClient.GetAsync(
                $"{BaseUrl}/api/customers?search={Uri.EscapeDataString(query)}&officeCode={_options.OfficeCode}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("[NebimERPAdapter] SearchAccounts failed: {Status} — {Error}",
                    (int)response.StatusCode, errorBody);
                return new List<ErpAccountResult>();
            }

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var results = new List<ErpAccountResult>();

            foreach (var item in json.RootElement.EnumerateArray())
            {
                var code = item.TryGetProperty("currAccCode", out var c) ? c.GetString() ?? "" : "";
                var name = item.TryGetProperty("currAccDescription", out var n) ? n.GetString() ?? "" : "";
                var balance = item.TryGetProperty("balance", out var b) ? b.GetDecimal() : 0m;
                var currency = item.TryGetProperty("currencyCode", out var cur)
                    ? cur.GetString() ?? "TRY" : "TRY";
                results.Add(ErpAccountResult.Ok(code, name, balance, currency));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] SearchAccountsAsync exception");
            return new List<ErpAccountResult>();
        }
#pragma warning restore CA1031
    }

    public async Task<decimal> GetAccountBalanceAsync(string accountCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accountCode)) return 0m;

#pragma warning disable CA1031 // Intentional: graceful degradation — return 0 on error
        try
        {
            SetApiKeyHeader();
            using var response = await _httpClient.GetAsync(
                $"{BaseUrl}/api/customers/{Uri.EscapeDataString(accountCode)}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("[NebimERPAdapter] GetAccountBalance failed: {Status} — {Error}",
                    (int)response.StatusCode, errorBody);
                return 0m;
            }

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            return json.RootElement.TryGetProperty("balance", out var b) ? b.GetDecimal() : 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] GetAccountBalanceAsync exception — {Code}", accountCode);
            return 0m;
        }
#pragma warning restore CA1031
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpWaybillCapable — Dalga 15: Nebim irsaliye yetkinligi
    // ═══════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ErpWaybillResult> CreateWaybillAsync(ErpWaybillRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[NebimERPAdapter] CreateWaybillAsync — Customer:{Customer}", request.CustomerCode);

#pragma warning disable CA1031 // Intentional: ERP failure must be returned, not propagated
        try
        {
            SetApiKeyHeader();

            var nebimDispatch = new
            {
                officeCode = _options.OfficeCode,
                warehouseCode = _options.WarehouseCode,
                currAccCode = request.CustomerCode,
                shippingAddress = request.ShippingAddress,
                carrierCode = request.CargoFirm,
                trackingNumber = request.TrackingNumber,
                dispatchDate = DateTime.Today.ToString("yyyy-MM-dd"),
                lines = request.Lines.Select(l => new
                {
                    productCode = l.ProductCode,
                    qty = l.Quantity,
                    unitCode = l.UnitCode
                }).ToArray()
            };

            var content = new StringContent(
                JsonSerializer.Serialize(nebimDispatch, JsonOptions),
                Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync($"{BaseUrl}/api/dispatches", content, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var json = JsonDocument.Parse(body).RootElement;

                var waybillNumber = json.TryGetProperty("dispatchNumber", out var dn)
                    ? dn.GetString() ?? string.Empty : string.Empty;
                var waybillDate = json.TryGetProperty("dispatchDate", out var dd) && DateTime.TryParse(dd.GetString(), out var dt)
                    ? dt : DateTime.Today;

                _logger.LogInformation("[NebimERPAdapter] Waybill created — Number:{Number}", waybillNumber);
                return ErpWaybillResult.Ok(waybillNumber, waybillDate);
            }

            var err = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("[NebimERPAdapter] CreateWaybill failed — HTTP {Status}: {Error}",
                (int)response.StatusCode, err[..Math.Min(200, err.Length)]);
            return ErpWaybillResult.Failed($"HTTP {(int)response.StatusCode}: {err[..Math.Min(100, err.Length)]}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] CreateWaybillAsync exception");
            return ErpWaybillResult.Failed(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc />
    public async Task<ErpWaybillResult?> GetWaybillAsync(string waybillNumber, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(waybillNumber);
        _logger.LogInformation("[NebimERPAdapter] GetWaybillAsync — Number:{Number}", waybillNumber);

#pragma warning disable CA1031 // Intentional: graceful degradation — return null on error
        try
        {
            SetApiKeyHeader();
            using var response = await _httpClient.GetAsync(
                $"{BaseUrl}/api/dispatches/{Uri.EscapeDataString(waybillNumber)}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("[NebimERPAdapter] GetWaybill — HTTP {Status}: {Error}",
                    (int)response.StatusCode, errorBody);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var json = JsonDocument.Parse(body).RootElement;

            var number = json.TryGetProperty("dispatchNumber", out var dn)
                ? dn.GetString() ?? waybillNumber : waybillNumber;
            var date = json.TryGetProperty("dispatchDate", out var dd) && DateTime.TryParse(dd.GetString(), out var dt)
                ? dt : DateTime.Today;

            return ErpWaybillResult.Ok(number, date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] GetWaybillAsync exception — Number:{Number}", waybillNumber);
            return null;
        }
#pragma warning restore CA1031
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private void SetApiKeyHeader()
    {
        var apiKey = !string.IsNullOrWhiteSpace(_options.ApiKey)
            ? _options.ApiKey
            : throw new InvalidOperationException("ERP:Nebim:ApiKey is not configured.");

        // Remove and re-add to avoid duplicates on reused HttpClient
        _httpClient.DefaultRequestHeaders.Remove("X-Nebim-ApiKey");
        _httpClient.DefaultRequestHeaders.Add("X-Nebim-ApiKey", apiKey);

        // Set database code header if configured
        if (!string.IsNullOrWhiteSpace(_options.DatabaseCode))
        {
            _httpClient.DefaultRequestHeaders.Remove("X-Nebim-Database");
            _httpClient.DefaultRequestHeaders.Add("X-Nebim-Database", _options.DatabaseCode);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpPriceCapable — Nebim price capability
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retrieves all product prices from Nebim catalog.
    /// GET /api/products
    /// </summary>
    public async Task<List<ErpPriceItem>> GetProductPricesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[NebimERPAdapter] GetProductPricesAsync");

#pragma warning disable CA1031 // Intentional: graceful degradation — return empty on error
        try
        {
            SetApiKeyHeader();

            var url = $"{BaseUrl}/api/products?officeCode={_options.OfficeCode}";
            using var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning(
                    "[NebimERPAdapter] GetProductPrices failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return new List<ErpPriceItem>();
            }

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var items = new List<ErpPriceItem>();

            foreach (var item in json.RootElement.EnumerateArray())
            {
                var productCode = item.TryGetProperty("productCode", out var pc)
                    ? pc.GetString() ?? string.Empty : string.Empty;
                var productName = item.TryGetProperty("productDescription", out var pd)
                    ? pd.GetString() ?? string.Empty : string.Empty;
                var purchasePrice = item.TryGetProperty("purchasePrice", out var pp)
                    ? pp.GetDecimal() : 0m;
                var salePrice = item.TryGetProperty("retailPrice", out var rp)
                    ? rp.GetDecimal() : 0m;
                var listPrice = item.TryGetProperty("listPrice", out var lp)
                    ? (decimal?)lp.GetDecimal() : null;
                var currency = item.TryGetProperty("currencyCode", out var cc)
                    ? cc.GetString() ?? "TRY" : "TRY";

                items.Add(new ErpPriceItem(productCode, productName, purchasePrice, salePrice, listPrice, currency));
            }

            _logger.LogInformation(
                "[NebimERPAdapter] Retrieved {Count} price items from Nebim", items.Count);
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] GetProductPricesAsync exception");
            return new List<ErpPriceItem>();
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Retrieves price for a specific product code.
    /// GET /api/products/{productCode}
    /// </summary>
    public async Task<ErpPriceItem?> GetPriceByCodeAsync(string productCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);

#pragma warning disable CA1031
        try
        {
            SetApiKeyHeader();

            var url = $"{BaseUrl}/api/products/{Uri.EscapeDataString(productCode)}";
            using var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var item = json.RootElement;

            var name = item.TryGetProperty("productDescription", out var pd)
                ? pd.GetString() ?? string.Empty : string.Empty;
            var purchasePrice = item.TryGetProperty("purchasePrice", out var pp)
                ? pp.GetDecimal() : 0m;
            var salePrice = item.TryGetProperty("retailPrice", out var rp)
                ? rp.GetDecimal() : 0m;
            var listPrice = item.TryGetProperty("listPrice", out var lp)
                ? (decimal?)lp.GetDecimal() : null;
            var currency = item.TryGetProperty("currencyCode", out var cc)
                ? cc.GetString() ?? "TRY" : "TRY";

            return new ErpPriceItem(productCode, name, purchasePrice, salePrice, listPrice, currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] GetPriceByCodeAsync exception for {Code}", productCode);
            return null;
        }
#pragma warning restore CA1031
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpBankCapable — Nebim bank capability
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retrieves bank transactions from Nebim.
    /// GET /api/payments?from={from}&amp;to={to}
    /// </summary>
    public async Task<List<ErpBankTransaction>> GetTransactionsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        _logger.LogInformation("[NebimERPAdapter] GetTransactionsAsync — From:{From} To:{To}", from, to);

#pragma warning disable CA1031
        try
        {
            SetApiKeyHeader();

            var fromStr = from.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var toStr = to.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var url = $"{BaseUrl}/api/payments?startDate={fromStr}&endDate={toStr}&officeCode={_options.OfficeCode}";
            using var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("[NebimERPAdapter] GetTransactions failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return new List<ErpBankTransaction>();
            }

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var items = new List<ErpBankTransaction>();

            foreach (var item in json.RootElement.EnumerateArray())
            {
                var txDate = item.TryGetProperty("paymentDate", out var pd) && DateTime.TryParse(pd.GetString(), out var dt)
                    ? dt : DateTime.Today;
                var amount = item.TryGetProperty("amount", out var a) ? a.GetDecimal() : 0m;
                var desc = item.TryGetProperty("description", out var d) ? d.GetString() ?? string.Empty : string.Empty;
                var txType = item.TryGetProperty("paymentType", out var pt) ? pt.GetString() ?? "OTHER" : "OTHER";
                var reference = item.TryGetProperty("documentNumber", out var dn) ? dn.GetString() : null;

                items.Add(new ErpBankTransaction(txDate, amount, desc, txType, reference));
            }

            _logger.LogInformation("[NebimERPAdapter] Retrieved {Count} bank transactions", items.Count);
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] GetTransactionsAsync exception");
            return new List<ErpBankTransaction>();
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Records a payment in Nebim.
    /// POST /api/payments
    /// </summary>
    public async Task<ErpPaymentResult> RecordPaymentAsync(ErpPaymentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[NebimERPAdapter] RecordPayment — Account:{Account} Amount:{Amount}",
            request.AccountCode, request.Amount);

#pragma warning disable CA1031
        try
        {
            SetApiKeyHeader();

            var payload = new
            {
                currAccCode = request.AccountCode,
                amount = request.Amount,
                paymentType = request.PaymentType,
                dueDate = request.DueDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                description = request.Description,
                officeCode = _options.OfficeCode
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync($"{BaseUrl}/api/payments", content, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var json = JsonDocument.Parse(body).RootElement;
                var reference = json.TryGetProperty("documentNumber", out var dn) ? dn.GetString() ?? string.Empty : string.Empty;
                _logger.LogInformation("[NebimERPAdapter] Payment recorded — Ref:{Ref}", reference);
                return ErpPaymentResult.Ok(reference);
            }

            var err = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("[NebimERPAdapter] RecordPayment failed: {Status} — {Error}",
                (int)response.StatusCode, err);
            return ErpPaymentResult.Failed($"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NebimERPAdapter] RecordPayment exception");
            return ErpPaymentResult.Failed(ex.Message);
        }
#pragma warning restore CA1031
    }
}
