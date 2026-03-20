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
public sealed class NetsisERPAdapter : IErpAdapter, IErpInvoiceCapable, IErpAccountCapable, IErpStockCapable, IErpWaybillCapable, IErpBankCapable
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

    // ═══════════════════════════════════════════════════════════════════
    // IErpInvoiceCapable — Dalga 14: Netsis fatura yetkinligi
    // ═══════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ErpInvoiceResult> CreateInvoiceAsync(ErpInvoiceRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[NetsisERPAdapter] CreateInvoiceAsync — Customer:{Customer}", request.CustomerCode);

#pragma warning disable CA1031 // Intentional: ERP failure must be returned, not propagated
        try
        {
            SetBasicAuthHeader();

            var netsisInvoice = new
            {
                cariKod = request.CustomerCode,
                cariAd = request.CustomerName,
                vergiNo = request.TaxId,
                tarih = DateTime.Today.ToString("dd.MM.yyyy"),
                tip = "SATIS",
                paraBirimi = request.Currency,
                satirlar = request.Lines.Select(l => new
                {
                    stokkod = l.ProductCode,
                    stokAd = l.ProductName,
                    miktar = l.Quantity,
                    fiyat = (double)l.UnitPrice,
                    kdvOran = l.TaxRate,
                    iskonto = l.DiscountAmount.HasValue ? (double)l.DiscountAmount.Value : 0d
                }).ToArray(),
                araToplam = (double)request.SubTotal,
                kdvToplam = (double)request.TaxTotal,
                genelToplam = (double)request.GrandTotal,
                aciklama = request.Notes
            };

            var content = new StringContent(
                JsonSerializer.Serialize(netsisInvoice, JsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}/faturalar", content, ct);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var json = JsonDocument.Parse(body);

                var invoiceNumber = json.RootElement.TryGetProperty("faturaNo", out var fn)
                    ? fn.GetString() ?? string.Empty : string.Empty;
                var erpRef = json.RootElement.TryGetProperty("belgeNo", out var bn)
                    ? bn.GetString() ?? invoiceNumber : invoiceNumber;
                var grandTotal = json.RootElement.TryGetProperty("genelToplam", out var gt)
                    ? gt.GetDecimal() : request.GrandTotal;
                var pdfUrl = json.RootElement.TryGetProperty("pdfUrl", out var pu)
                    ? pu.GetString() : null;

                _logger.LogInformation(
                    "[NetsisERPAdapter] Invoice created — Number:{Number} Ref:{Ref}",
                    invoiceNumber, erpRef);
                return ErpInvoiceResult.Ok(invoiceNumber, erpRef, DateTime.Today, grandTotal, pdfUrl);
            }

            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "[NetsisERPAdapter] CreateInvoice failed — HTTP {Status}: {Error}",
                (int)response.StatusCode, err[..Math.Min(200, err.Length)]);
            return ErpInvoiceResult.Failed($"HTTP {(int)response.StatusCode}: {err[..Math.Min(100, err.Length)]}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] CreateInvoiceAsync exception");
            return ErpInvoiceResult.Failed(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc />
    public async Task<ErpInvoiceResult?> GetInvoiceAsync(string invoiceNumber, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        _logger.LogInformation("[NetsisERPAdapter] GetInvoiceAsync — Number:{Number}", invoiceNumber);

#pragma warning disable CA1031
        try
        {
            SetBasicAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/faturalar/{invoiceNumber}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[NetsisERPAdapter] GetInvoice — HTTP {Status}", (int)response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body).RootElement;

            var number = json.TryGetProperty("faturaNo", out var fn) ? fn.GetString() ?? invoiceNumber : invoiceNumber;
            var erpRef = json.TryGetProperty("belgeNo", out var bn) ? bn.GetString() ?? number : number;
            var date = json.TryGetProperty("tarih", out var t) && DateTime.TryParse(t.GetString(), out var dt)
                ? dt : DateTime.Today;
            var grandTotal = json.TryGetProperty("genelToplam", out var gt) ? gt.GetDecimal() : 0m;
            var pdfUrl = json.TryGetProperty("pdfUrl", out var pu) ? pu.GetString() : null;

            return ErpInvoiceResult.Ok(number, erpRef, date, grandTotal, pdfUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] GetInvoiceAsync exception — Number:{Number}", invoiceNumber);
            return null;
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc />
    public async Task<List<ErpInvoiceResult>> GetInvoicesAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        _logger.LogInformation("[NetsisERPAdapter] GetInvoicesAsync — From:{From} To:{To}", from, to);

#pragma warning disable CA1031
        try
        {
            SetBasicAuthHeader();
            var fromStr = from.ToString("yyyy-MM-dd");
            var toStr = to.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync(
                $"{BaseUrl}/faturalar?baslangic={fromStr}&bitis={toStr}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[NetsisERPAdapter] GetInvoices — HTTP {Status}", (int)response.StatusCode);
                return [];
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body);
            var results = new List<ErpInvoiceResult>();

            foreach (var item in json.RootElement.EnumerateArray())
            {
                var number = item.TryGetProperty("faturaNo", out var fn) ? fn.GetString() ?? string.Empty : string.Empty;
                var erpRef = item.TryGetProperty("belgeNo", out var bn) ? bn.GetString() ?? number : number;
                var date = item.TryGetProperty("tarih", out var t) && DateTime.TryParse(t.GetString(), out var dt)
                    ? dt : DateTime.Today;
                var grandTotal = item.TryGetProperty("genelToplam", out var gt) ? gt.GetDecimal() : 0m;
                var pdfUrl = item.TryGetProperty("pdfUrl", out var pu) ? pu.GetString() : null;

                results.Add(ErpInvoiceResult.Ok(number, erpRef, date, grandTotal, pdfUrl));
            }

            _logger.LogInformation("[NetsisERPAdapter] Retrieved {Count} invoices", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] GetInvoicesAsync exception");
            return [];
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc />
    public async Task<bool> CancelInvoiceAsync(string invoiceNumber, string reason, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        _logger.LogInformation("[NetsisERPAdapter] CancelInvoiceAsync — Number:{Number} Reason:{Reason}",
            invoiceNumber, reason);

#pragma warning disable CA1031
        try
        {
            SetBasicAuthHeader();
            var payload = new { iptalNedeni = reason };
            var content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/faturalar/{invoiceNumber}")
            {
                Content = content
            };
            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[NetsisERPAdapter] Invoice cancelled — Number:{Number}", invoiceNumber);
                return true;
            }

            _logger.LogWarning("[NetsisERPAdapter] CancelInvoice failed — HTTP {Status}", (int)response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] CancelInvoiceAsync exception — Number:{Number}", invoiceNumber);
            return false;
        }
#pragma warning restore CA1031
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpAccountCapable — Dalga 14: Netsis cari hesap yetkinligi
    // ═══════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ErpAccountResult> CreateAccountAsync(ErpAccountRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[NetsisERPAdapter] CreateAccountAsync — Code:{Code}", request.AccountCode);

#pragma warning disable CA1031
        try
        {
            SetBasicAuthHeader();

            var netsisCari = new
            {
                cariKod = request.AccountCode,
                cariAd = request.CompanyName,
                vergiNo = request.TaxId,
                vergiDairesi = request.TaxOffice,
                adres = request.Address,
                sehir = request.City,
                telefon = request.Phone,
                email = request.Email
            };

            var content = new StringContent(
                JsonSerializer.Serialize(netsisCari, JsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}/cariler", content, ct);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var json = JsonDocument.Parse(body).RootElement;

                var code = json.TryGetProperty("cariKod", out var ck) ? ck.GetString() ?? request.AccountCode : request.AccountCode;
                var name = json.TryGetProperty("cariAd", out var ca) ? ca.GetString() ?? request.CompanyName : request.CompanyName;
                var balance = json.TryGetProperty("bakiye", out var b) ? b.GetDecimal() : 0m;

                _logger.LogInformation("[NetsisERPAdapter] Account created — Code:{Code}", code);
                return ErpAccountResult.Ok(code, name, balance);
            }

            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("[NetsisERPAdapter] CreateAccount failed — HTTP {Status}: {Error}",
                (int)response.StatusCode, err[..Math.Min(200, err.Length)]);
            return ErpAccountResult.Failed($"HTTP {(int)response.StatusCode}: {err[..Math.Min(100, err.Length)]}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] CreateAccountAsync exception");
            return ErpAccountResult.Failed(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc />
    public async Task<ErpAccountResult?> GetAccountAsync(string accountCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountCode);
        _logger.LogInformation("[NetsisERPAdapter] GetAccountAsync — Code:{Code}", accountCode);

#pragma warning disable CA1031
        try
        {
            SetBasicAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/cariler/{accountCode}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[NetsisERPAdapter] GetAccount — HTTP {Status}", (int)response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body).RootElement;

            var code = json.TryGetProperty("cariKod", out var ck) ? ck.GetString() ?? accountCode : accountCode;
            var name = json.TryGetProperty("cariAd", out var ca) ? ca.GetString() ?? string.Empty : string.Empty;
            var balance = json.TryGetProperty("bakiye", out var b) ? b.GetDecimal() : 0m;
            var currency = json.TryGetProperty("paraBirimi", out var pb) ? pb.GetString() ?? "TRY" : "TRY";

            return ErpAccountResult.Ok(code, name, balance, currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] GetAccountAsync exception — Code:{Code}", accountCode);
            return null;
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc />
    public async Task<ErpAccountResult> UpdateAccountAsync(ErpAccountRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[NetsisERPAdapter] UpdateAccountAsync — Code:{Code}", request.AccountCode);

#pragma warning disable CA1031
        try
        {
            SetBasicAuthHeader();

            var netsisCari = new
            {
                cariKod = request.AccountCode,
                cariAd = request.CompanyName,
                vergiNo = request.TaxId,
                vergiDairesi = request.TaxOffice,
                adres = request.Address,
                sehir = request.City,
                telefon = request.Phone,
                email = request.Email
            };

            var content = new StringContent(
                JsonSerializer.Serialize(netsisCari, JsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{BaseUrl}/cariler/{request.AccountCode}", content, ct);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var json = JsonDocument.Parse(body).RootElement;

                var code = json.TryGetProperty("cariKod", out var ck) ? ck.GetString() ?? request.AccountCode : request.AccountCode;
                var name = json.TryGetProperty("cariAd", out var ca) ? ca.GetString() ?? request.CompanyName : request.CompanyName;
                var balance = json.TryGetProperty("bakiye", out var b) ? b.GetDecimal() : 0m;

                _logger.LogInformation("[NetsisERPAdapter] Account updated — Code:{Code}", code);
                return ErpAccountResult.Ok(code, name, balance);
            }

            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("[NetsisERPAdapter] UpdateAccount failed — HTTP {Status}: {Error}",
                (int)response.StatusCode, err[..Math.Min(200, err.Length)]);
            return ErpAccountResult.Failed($"HTTP {(int)response.StatusCode}: {err[..Math.Min(100, err.Length)]}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] UpdateAccountAsync exception");
            return ErpAccountResult.Failed(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc />
    public async Task<List<ErpAccountResult>> SearchAccountsAsync(string query, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        _logger.LogInformation("[NetsisERPAdapter] SearchAccountsAsync — Query:{Query}", query);

#pragma warning disable CA1031
        try
        {
            SetBasicAuthHeader();
            var response = await _httpClient.GetAsync(
                $"{BaseUrl}/cariler?arama={Uri.EscapeDataString(query)}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[NetsisERPAdapter] SearchAccounts — HTTP {Status}", (int)response.StatusCode);
                return [];
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body);
            var results = new List<ErpAccountResult>();

            foreach (var item in json.RootElement.EnumerateArray())
            {
                var code = item.TryGetProperty("cariKod", out var ck) ? ck.GetString() ?? string.Empty : string.Empty;
                var name = item.TryGetProperty("cariAd", out var ca) ? ca.GetString() ?? string.Empty : string.Empty;
                var balance = item.TryGetProperty("bakiye", out var b) ? b.GetDecimal() : 0m;
                var currency = item.TryGetProperty("paraBirimi", out var pb) ? pb.GetString() ?? "TRY" : "TRY";

                results.Add(ErpAccountResult.Ok(code, name, balance, currency));
            }

            _logger.LogInformation("[NetsisERPAdapter] Found {Count} accounts for query '{Query}'", results.Count, query);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] SearchAccountsAsync exception");
            return [];
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc />
    public async Task<decimal> GetAccountBalanceAsync(string accountCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountCode);
        _logger.LogInformation("[NetsisERPAdapter] GetAccountBalanceAsync — Code:{Code}", accountCode);

#pragma warning disable CA1031
        try
        {
            var account = await GetAccountAsync(accountCode, ct);
            return account?.Balance ?? 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] GetAccountBalanceAsync exception — Code:{Code}", accountCode);
            return 0m;
        }
#pragma warning restore CA1031
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpStockCapable — Dalga 14: Netsis stok yetkinligi
    // ═══════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<List<ErpStockItem>> GetStockLevelsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[NetsisERPAdapter] GetStockLevelsAsync");

#pragma warning disable CA1031
        try
        {
            SetBasicAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/stoklar", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[NetsisERPAdapter] GetStockLevels — HTTP {Status}", (int)response.StatusCode);
                return [];
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body);
            var items = new List<ErpStockItem>();

            foreach (var item in json.RootElement.EnumerateArray())
            {
                var productCode = item.TryGetProperty("stokKod", out var sk) ? sk.GetString() ?? string.Empty : string.Empty;
                var productName = item.TryGetProperty("stokAd", out var sa) ? sa.GetString() ?? string.Empty : string.Empty;
                var quantity = item.TryGetProperty("miktar", out var m) ? m.GetInt32() : 0;
                var unitCode = item.TryGetProperty("birimKod", out var bk) ? bk.GetString() ?? "ADET" : "ADET";
                var warehouseCode = item.TryGetProperty("depoKod", out var dk) ? dk.GetString() : null;
                var unitCost = item.TryGetProperty("birimMaliyet", out var bm) ? (decimal?)bm.GetDecimal() : null;

                items.Add(new ErpStockItem(productCode, productName, quantity, unitCode, warehouseCode, unitCost));
            }

            _logger.LogInformation("[NetsisERPAdapter] Retrieved {Count} stock items", items.Count);
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] GetStockLevelsAsync exception");
            return [];
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc />
    public async Task<ErpStockItem?> GetStockByCodeAsync(string productCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);
        _logger.LogInformation("[NetsisERPAdapter] GetStockByCodeAsync — Code:{Code}", productCode);

#pragma warning disable CA1031
        try
        {
            SetBasicAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/stoklar/{productCode}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[NetsisERPAdapter] GetStockByCode — HTTP {Status}", (int)response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body).RootElement;

            var code = json.TryGetProperty("stokKod", out var sk) ? sk.GetString() ?? productCode : productCode;
            var name = json.TryGetProperty("stokAd", out var sa) ? sa.GetString() ?? string.Empty : string.Empty;
            var quantity = json.TryGetProperty("miktar", out var m) ? m.GetInt32() : 0;
            var unitCode = json.TryGetProperty("birimKod", out var bk) ? bk.GetString() ?? "ADET" : "ADET";
            var warehouseCode = json.TryGetProperty("depoKod", out var dk) ? dk.GetString() : null;
            var unitCost = json.TryGetProperty("birimMaliyet", out var bm) ? (decimal?)bm.GetDecimal() : null;

            return new ErpStockItem(code, name, quantity, unitCode, warehouseCode, unitCost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] GetStockByCodeAsync exception — Code:{Code}", productCode);
            return null;
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc />
    public async Task<bool> UpdateStockAsync(string productCode, int quantity, string warehouseCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(warehouseCode);
        _logger.LogInformation(
            "[NetsisERPAdapter] UpdateStockAsync — Code:{Code} Qty:{Qty} Warehouse:{Warehouse}",
            productCode, quantity, warehouseCode);

#pragma warning disable CA1031
        try
        {
            SetBasicAuthHeader();

            var hareket = new
            {
                miktar = quantity,
                depoKod = warehouseCode,
                tarih = DateTime.Today.ToString("dd.MM.yyyy"),
                hareketTip = "GIRIS"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(hareket, JsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/stoklar/{productCode}/hareket", content, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "[NetsisERPAdapter] Stock updated — Code:{Code} Qty:{Qty}", productCode, quantity);
                return true;
            }

            _logger.LogWarning("[NetsisERPAdapter] UpdateStock failed — HTTP {Status}", (int)response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] UpdateStockAsync exception — Code:{Code}", productCode);
            return false;
        }
#pragma warning restore CA1031
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpWaybillCapable — Dalga 14: Netsis irsaliye yetkinligi
    // ═══════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ErpWaybillResult> CreateWaybillAsync(ErpWaybillRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[NetsisERPAdapter] CreateWaybillAsync — Customer:{Customer}", request.CustomerCode);

#pragma warning disable CA1031
        try
        {
            SetBasicAuthHeader();

            var netsisWaybill = new
            {
                cariKod = request.CustomerCode,
                sevkAdresi = request.ShippingAddress,
                kargoFirma = request.CargoFirm,
                takipNo = request.TrackingNumber,
                tarih = DateTime.Today.ToString("dd.MM.yyyy"),
                satirlar = request.Lines.Select(l => new
                {
                    stokkod = l.ProductCode,
                    miktar = l.Quantity,
                    birimKod = l.UnitCode
                }).ToArray()
            };

            var content = new StringContent(
                JsonSerializer.Serialize(netsisWaybill, JsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}/irsaliyeler", content, ct);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var json = JsonDocument.Parse(body).RootElement;

                var waybillNumber = json.TryGetProperty("irsaliyeNo", out var wn)
                    ? wn.GetString() ?? string.Empty : string.Empty;
                var waybillDate = json.TryGetProperty("tarih", out var t) && DateTime.TryParse(t.GetString(), out var dt)
                    ? dt : DateTime.Today;

                _logger.LogInformation("[NetsisERPAdapter] Waybill created — Number:{Number}", waybillNumber);
                return ErpWaybillResult.Ok(waybillNumber, waybillDate);
            }

            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("[NetsisERPAdapter] CreateWaybill failed — HTTP {Status}: {Error}",
                (int)response.StatusCode, err[..Math.Min(200, err.Length)]);
            return ErpWaybillResult.Failed($"HTTP {(int)response.StatusCode}: {err[..Math.Min(100, err.Length)]}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] CreateWaybillAsync exception");
            return ErpWaybillResult.Failed(ex.Message);
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc />
    public async Task<ErpWaybillResult?> GetWaybillAsync(string waybillNumber, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(waybillNumber);
        _logger.LogInformation("[NetsisERPAdapter] GetWaybillAsync — Number:{Number}", waybillNumber);

#pragma warning disable CA1031
        try
        {
            SetBasicAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/irsaliyeler/{waybillNumber}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[NetsisERPAdapter] GetWaybill — HTTP {Status}", (int)response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body).RootElement;

            var number = json.TryGetProperty("irsaliyeNo", out var wn)
                ? wn.GetString() ?? waybillNumber : waybillNumber;
            var date = json.TryGetProperty("tarih", out var t) && DateTime.TryParse(t.GetString(), out var dt)
                ? dt : DateTime.Today;

            return ErpWaybillResult.Ok(number, date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] GetWaybillAsync exception — Number:{Number}", waybillNumber);
            return null;
        }
#pragma warning restore CA1031
    }

    // ═══════════════════════════════════════════════════════════════════
    // IErpBankCapable — Dalga 15: Netsis banka yetkinligi
    // ═══════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<List<ErpBankTransaction>> GetTransactionsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        _logger.LogInformation("[NetsisERPAdapter] GetTransactionsAsync — From:{From} To:{To}", from, to);

#pragma warning disable CA1031 // Intentional: graceful degradation — return empty on error
        try
        {
            SetBasicAuthHeader();

            var fromStr = from.ToString("yyyy-MM-dd");
            var toStr = to.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync(
                $"{BaseUrl}/api/bankTransaction?startDate={fromStr}&endDate={toStr}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[NetsisERPAdapter] GetTransactions — HTTP {Status}", (int)response.StatusCode);
                return [];
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body);
            var transactions = new List<ErpBankTransaction>();

            foreach (var item in json.RootElement.EnumerateArray())
            {
                var date = item.TryGetProperty("tarih", out var t) && DateTime.TryParse(t.GetString(), out var dt)
                    ? dt : DateTime.Today;
                var amount = item.TryGetProperty("tutar", out var a) ? a.GetDecimal() : 0m;
                var description = item.TryGetProperty("aciklama", out var d) ? d.GetString() ?? string.Empty : string.Empty;
                var transactionType = item.TryGetProperty("hareketTip", out var ht) ? ht.GetString() ?? string.Empty : string.Empty;
                var reference = item.TryGetProperty("referans", out var r) ? r.GetString() : null;

                transactions.Add(new ErpBankTransaction(date, amount, description, transactionType, reference));
            }

            _logger.LogInformation("[NetsisERPAdapter] Retrieved {Count} bank transactions", transactions.Count);
            return transactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] GetTransactionsAsync exception");
            return [];
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc />
    public async Task<ErpPaymentResult> RecordPaymentAsync(ErpPaymentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[NetsisERPAdapter] RecordPaymentAsync — Account:{Account} Amount:{Amount}",
            request.AccountCode, request.Amount);

#pragma warning disable CA1031 // Intentional: ERP failure must be returned, not propagated
        try
        {
            SetBasicAuthHeader();

            var netsisPayment = new
            {
                cariKod = request.AccountCode,
                tutar = (double)request.Amount,
                odemeTip = request.PaymentType,
                vadeTarih = request.DueDate?.ToString("dd.MM.yyyy"),
                aciklama = request.Description
            };

            var content = new StringContent(
                JsonSerializer.Serialize(netsisPayment, JsonOptions),
                Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}/api/payment", content, ct);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var json = JsonDocument.Parse(body).RootElement;

                var reference = json.TryGetProperty("referans", out var r)
                    ? r.GetString() ?? string.Empty : string.Empty;

                _logger.LogInformation("[NetsisERPAdapter] Payment recorded — Reference:{Reference}", reference);
                return ErpPaymentResult.Ok(reference);
            }

            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("[NetsisERPAdapter] RecordPayment failed — HTTP {Status}: {Error}",
                (int)response.StatusCode, err[..Math.Min(200, err.Length)]);
            return ErpPaymentResult.Failed($"HTTP {(int)response.StatusCode}: {err[..Math.Min(100, err.Length)]}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetsisERPAdapter] RecordPaymentAsync exception");
            return ErpPaymentResult.Failed(ex.Message);
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
