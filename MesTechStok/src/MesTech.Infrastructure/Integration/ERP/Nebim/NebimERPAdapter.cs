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
/// Implements IErpAdapter (Dalga 11 enum-based interface) + IErpStockCapable.
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
public sealed class NebimERPAdapter : IErpAdapter, IErpStockCapable
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
            var order = await _orderRepository.GetByIdAsync(orderId);
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

            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/api/invoices", content, ct);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var json = JsonDocument.Parse(body);
                var erpRef = json.RootElement.TryGetProperty("invoiceNumber", out var inv)
                    ? inv.GetString() ?? "OK" : "OK";

                _logger.LogInformation(
                    "[NebimERPAdapter] Order synced — OrderId:{OrderId} NebimRef:{ErpRef}",
                    orderId, erpRef);
                return ErpSyncResult.Ok(erpRef);
            }

            var err = await response.Content.ReadAsStringAsync(ct);
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

            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/api/invoices", content, ct);

            if (response.IsSuccessStatusCode)
            {
                var erpRef = $"NEBIM-INV-{invoiceId:N}"[..32];
                _logger.LogInformation(
                    "[NebimERPAdapter] Invoice synced — InvoiceId:{InvoiceId} NebimRef:{ErpRef}",
                    invoiceId, erpRef);
                return ErpSyncResult.Ok(erpRef);
            }

            var err = await response.Content.ReadAsStringAsync(ct);
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

            var response = await _httpClient.GetAsync(
                $"{BaseUrl}/api/customers?officeCode={_options.OfficeCode}&limit=200", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "[NebimERPAdapter] GetAccountBalances failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return Array.Empty<ErpAccountDto>();
            }

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
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
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/ping", ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[NebimERPAdapter] Ping OK");
                return true;
            }

            _logger.LogWarning("[NebimERPAdapter] Ping failed: {Status}", response.StatusCode);
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
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "[NebimERPAdapter] GetStockLevels failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return new List<ErpStockItem>();
            }

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
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
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
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

            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/api/inventory/levels", content, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "[NebimERPAdapter] Stock updated — ProductCode:{ProductCode} Qty:{Quantity}",
                    productCode, quantity);
                return true;
            }

            var err = await response.Content.ReadAsStringAsync(ct);
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
}
