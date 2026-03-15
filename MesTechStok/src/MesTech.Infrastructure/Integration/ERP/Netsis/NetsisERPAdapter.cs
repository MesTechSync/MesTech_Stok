using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.ERP.Netsis;

/// <summary>
/// Netsis ERP adapter — Netsis Enterprise / Wings REST API.
/// Auth: Basic Auth (username + password).
/// Implements IErpAdapter (Dalga 11 enum-based interface).
///
/// Config keys:
///   - ERP:Netsis:BaseUrl   (e.g. "https://FIRMA.netsis.com.tr/api/v2")
///   - ERP:Netsis:Username
///   - ERP:Netsis:Password
///
/// Netsis API endpoints:
///   - POST /siparisler   (order sync)
///   - POST /faturalar    (invoice sync)
///   - GET  /cariler      (account balances)
///   - GET  /ping         (health check)
/// </summary>
public sealed class NetsisERPAdapter : IErpAdapter
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<NetsisERPAdapter> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ErpProvider Provider => ErpProvider.Netsis;

    public NetsisERPAdapter(
        HttpClient httpClient,
        IConfiguration config,
        IOrderRepository orderRepository,
        ILogger<NetsisERPAdapter> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private string BaseUrl => _config["ERP:Netsis:BaseUrl"]
        ?? throw new InvalidOperationException("ERP:Netsis:BaseUrl is not configured.");

    // ═══════════════════════════════════════════════════════════════════
    // IErpAdapter — Dalga 13: Netsis ERP integration
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Syncs a MesTech Order to Netsis as a sales order.
    /// Fetches Order by ID, maps to Netsis format, POSTs to /siparisler.
    /// </summary>
    public async Task<ErpSyncResult> SyncOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        if (orderId == Guid.Empty)
            return ErpSyncResult.Fail("OrderId cannot be empty.");

        _logger.LogInformation(
            "[NetsisERPAdapter] SyncOrderAsync — OrderId:{OrderId}", orderId);

#pragma warning disable CA1031 // Intentional: ERP sync failure must be returned, not propagated
        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order is null)
            {
                _logger.LogWarning(
                    "[NetsisERPAdapter] Order not found: {OrderId}", orderId);
                return ErpSyncResult.Fail($"Order not found: {orderId}");
            }

            SetBasicAuthHeader();

            var firstItem = order.OrderItems.FirstOrDefault();
            var netsisOrder = new
            {
                belgeNo = order.OrderNumber,
                tarih = order.OrderDate.ToString("dd.MM.yyyy"),
                cariKod = "ETICARET",
                satirlar = new[]
                {
                    new
                    {
                        stokkod = firstItem?.ProductSKU ?? "GENEL",
                        miktar = firstItem?.Quantity ?? 1,
                        fiyat = (double)order.TotalAmount,
                        kdvOran = 20
                    }
                },
                aciklama = $"MesTech Siparis #{order.OrderNumber}"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(netsisOrder, JsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/siparisler", content, ct);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var json = JsonDocument.Parse(body);
                var erpRef = json.RootElement.TryGetProperty("belgeNo", out var bn)
                    ? bn.GetString() ?? "OK" : "OK";

                _logger.LogInformation(
                    "[NetsisERPAdapter] Order synced — OrderId:{OrderId} NetsisRef:{ErpRef}",
                    orderId, erpRef);
                return ErpSyncResult.Ok(erpRef);
            }

            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "[NetsisERPAdapter] Order sync failed — OrderId:{OrderId} HTTP {Status}: {Error}",
                orderId, (int)response.StatusCode, err[..Math.Min(200, err.Length)]);
            return ErpSyncResult.Fail(
                $"HTTP {(int)response.StatusCode}: {err[..Math.Min(100, err.Length)]}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[NetsisERPAdapter] SyncOrderAsync exception — OrderId:{OrderId}", orderId);
            return ErpSyncResult.Fail(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Syncs a MesTech Invoice to Netsis.
    /// POSTs to /faturalar.
    /// </summary>
    public async Task<ErpSyncResult> SyncInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (invoiceId == Guid.Empty)
            return ErpSyncResult.Fail("InvoiceId cannot be empty.");

        _logger.LogInformation(
            "[NetsisERPAdapter] SyncInvoiceAsync — InvoiceId:{InvoiceId}", invoiceId);

#pragma warning disable CA1031 // Intentional: ERP sync failure must be returned, not propagated
        try
        {
            SetBasicAuthHeader();

            var netsisInvoice = new
            {
                faturaNo = invoiceId.ToString("N")[..16],
                tarih = DateTime.Today.ToString("dd.MM.yyyy"),
                tip = "SATIS"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(netsisInvoice, JsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/faturalar", content, ct);

            if (response.IsSuccessStatusCode)
            {
                var erpRef = $"NETSIS-INV-{invoiceId:N}"[..32];
                _logger.LogInformation(
                    "[NetsisERPAdapter] Invoice synced — InvoiceId:{InvoiceId} NetsisRef:{ErpRef}",
                    invoiceId, erpRef);
                return ErpSyncResult.Ok(erpRef);
            }

            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "[NetsisERPAdapter] Invoice sync failed — InvoiceId:{InvoiceId} HTTP {Status}: {Error}",
                invoiceId, (int)response.StatusCode, err[..Math.Min(200, err.Length)]);
            return ErpSyncResult.Fail($"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[NetsisERPAdapter] SyncInvoiceAsync exception — InvoiceId:{InvoiceId}", invoiceId);
            return ErpSyncResult.Fail(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Retrieves account balances from Netsis.
    /// GET /cariler?limit=200 — parses JSON array of cari records.
    /// </summary>
    public async Task<IReadOnlyList<ErpAccountDto>> GetAccountBalancesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[NetsisERPAdapter] GetAccountBalancesAsync");

#pragma warning disable CA1031 // Intentional: graceful degradation — return empty on error
        try
        {
            SetBasicAuthHeader();

            var response = await _httpClient.GetAsync(
                $"{BaseUrl}/cariler?limit=200", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "[NetsisERPAdapter] GetAccountBalances failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return Array.Empty<ErpAccountDto>();
            }

            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var accounts = new List<ErpAccountDto>();

            foreach (var item in json.RootElement.EnumerateArray())
            {
                var accountCode = item.TryGetProperty("cariKod", out var ck)
                    ? ck.GetString() ?? string.Empty : string.Empty;
                var accountName = item.TryGetProperty("cariAd", out var ca)
                    ? ca.GetString() ?? string.Empty : string.Empty;
                var balance = item.TryGetProperty("bakiye", out var b)
                    ? b.GetDecimal() : 0m;

                accounts.Add(new ErpAccountDto(accountCode, accountName, balance, "TRY"));
            }

            _logger.LogInformation(
                "[NetsisERPAdapter] Retrieved {Count} account balances from Netsis",
                accounts.Count);

            return accounts.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] GetAccountBalancesAsync exception");
            return Array.Empty<ErpAccountDto>();
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Health check for Netsis REST API.
    /// GET /ping — returns true if server responds with 2xx.
    /// </summary>
    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
#pragma warning disable CA1031 // Intentional: health check must not throw
        try
        {
            SetBasicAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/ping", ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[NetsisERPAdapter] Ping OK");
                return true;
            }

            _logger.LogWarning("[NetsisERPAdapter] Ping failed: {Status}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] Ping exception");
            return false;
        }
#pragma warning restore CA1031
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private void SetBasicAuthHeader()
    {
        var username = _config["ERP:Netsis:Username"]
            ?? throw new InvalidOperationException("ERP:Netsis:Username is not configured.");
        var password = _config["ERP:Netsis:Password"]
            ?? throw new InvalidOperationException("ERP:Netsis:Password is not configured.");

        var encoded = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{username}:{password}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", encoded);
    }
}
