using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// Hepsiburada e-Fatura entegrasyonu — platform-dahili REST JSON API.
/// Desteklenen islemler: e-Fatura, e-Arsiv, e-Irsaliye, durum sorgulama, PDF, iptal,
/// toplu fatura, kontor bakiye.
/// Auth: HTTP Basic Auth — Authorization: Basic Base64(username:apiKey).
/// URL pattern: {baseUrl}/invoice/api/v1/...
/// </summary>
public class HBFaturaProvider : IInvoiceProvider, IBulkInvoiceCapable, IKontorCapable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HBFaturaProvider> _logger;
    private string? _username;
    private string? _apiKey;
    private string? _baseUrl;
    private bool _isConfigured;

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string ProviderName => "Hepsiburada Fatura";
    public InvoiceProvider Provider => InvoiceProvider.HepsiburadaFatura;

    public HBFaturaProvider(HttpClient httpClient, ILogger<HBFaturaProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Username + ApiKey + Base URL ile provider'i yapilandirir.
    /// Auth: Authorization: Basic Base64(username:apiKey).
    /// </summary>
    public void Configure(string username, string apiKey, string baseUrl)
    {
        _username = username ?? throw new ArgumentNullException(nameof(username));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        _isConfigured = true;

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_apiKey}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        _logger.LogInformation("HBFaturaProvider configured for {BaseUrl}", _baseUrl);
    }

    // ── IInvoiceProvider ─────────────────────────────────────────────────

    public async Task<InvoiceResult> CreateEFaturaAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("HBFatura CreateEFatura for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildInvoicePayload(invoice, "EFATURA");
        return await PostInvoiceAsync($"{_baseUrl}/invoice/api/v1/efatura", payload, ct);
    }

    public async Task<InvoiceResult> CreateEArsivAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("HBFatura CreateEArsiv for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildInvoicePayload(invoice, "EARSIV");
        return await PostInvoiceAsync($"{_baseUrl}/invoice/api/v1/earsiv", payload, ct);
    }

    public async Task<InvoiceResult> CreateEIrsaliyeAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("HBFatura CreateEIrsaliye for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildDispatchPayload(invoice);
        return await PostInvoiceAsync($"{_baseUrl}/invoice/api/v1/eirsaliye", payload, ct);
    }

    public async Task<InvoiceStatusResult> CheckStatusAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("HBFatura CheckStatus for {GibInvoiceId}", gibInvoiceId);

        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/invoice/api/v1/invoices/{gibInvoiceId}/status", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("HBFatura CheckStatus failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return new InvoiceStatusResult(gibInvoiceId, "Error", null, errorBody);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "Unknown" : "Unknown";
            DateTime? acceptedAt = root.TryGetProperty("acceptedAt", out var a) && a.ValueKind != JsonValueKind.Null
                ? a.GetDateTime()
                : null;
            var error = root.TryGetProperty("errorMessage", out var e) ? e.GetString() : null;

            return new InvoiceStatusResult(gibInvoiceId, status, acceptedAt, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HBFatura CheckStatus exception for {GibInvoiceId}", gibInvoiceId);
            return new InvoiceStatusResult(gibInvoiceId, "Error", null, ex.Message);
        }
    }

    public async Task<byte[]> GetPdfAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("HBFatura GetPdf for {GibInvoiceId}", gibInvoiceId);

        var response = await _httpClient.GetAsync(
            $"{_baseUrl}/invoice/api/v1/invoices/{gibInvoiceId}/pdf", ct);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<bool> IsEInvoiceTaxpayerAsync(string taxNumber, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("HBFatura IsEInvoiceTaxpayer check for {TaxNumber}", taxNumber);

        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/invoice/api/v1/taxpayers/{taxNumber}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("HBFatura taxpayer check returned {Status} for {TaxNumber}",
                    response.StatusCode, taxNumber);
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.TryGetProperty("isRegistered", out var reg) && reg.GetBoolean();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HBFatura taxpayer check exception for {TaxNumber}", taxNumber);
            return false;
        }
    }

    public async Task<InvoiceResult> CancelInvoiceAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("HBFatura CancelInvoice for {GibInvoiceId}", gibInvoiceId);

        try
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/invoice/api/v1/invoices/{gibInvoiceId}/cancel", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("HBFatura CancelInvoice failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return new InvoiceResult(false, gibInvoiceId, null, errorBody);
            }

            return new InvoiceResult(true, gibInvoiceId, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HBFatura CancelInvoice exception for {GibInvoiceId}", gibInvoiceId);
            return new InvoiceResult(false, gibInvoiceId, null, ex.Message);
        }
    }

    // ── IBulkInvoiceCapable ──────────────────────────────────────────────

    public async Task<BulkInvoiceResult> CreateBulkInvoiceAsync(
        IEnumerable<InvoiceCreateRequest> requests, CancellationToken ct = default)
    {
        EnsureConfigured();
        var requestList = requests.ToList();
        _logger.LogInformation("HBFatura CreateBulkInvoice for {Count} invoices", requestList.Count);

        try
        {
            var payloads = requestList.Select(req => new
            {
                invoiceType = "EFATURA",
                platformOrderId = req.PlatformOrderId,
                customer = new
                {
                    name = req.Customer.Name,
                    taxNumber = req.Customer.TaxNumber,
                    taxOffice = req.Customer.TaxOffice,
                    address = req.Customer.Address,
                    email = req.Customer.Email,
                    phone = req.Customer.Phone
                },
                amounts = new
                {
                    grandTotal = req.TotalAmount
                },
                lines = req.Lines.Select(l => new
                {
                    productName = l.ProductName,
                    sku = l.SKU,
                    quantity = l.Quantity,
                    unitPrice = l.UnitPrice,
                    taxRate = l.TaxRate,
                    discountAmount = l.DiscountAmount
                }).ToArray(),
                note = req.Note
            }).ToArray();

            var payload = new { invoices = payloads };
            var json = JsonSerializer.Serialize(payload, CamelCaseOptions);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/invoice/api/v1/invoices/bulk", httpContent, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("HBFatura CreateBulkInvoice failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                var failResults = requestList.Select(r =>
                    new BulkInvoiceItemResult(r.OrderId, false, null, errorBody)).ToList();
                return new BulkInvoiceResult(requestList.Count, 0, requestList.Count, failResults);
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var results = new List<BulkInvoiceItemResult>();

            if (doc.RootElement.TryGetProperty("results", out var resultsArray))
            {
                var i = 0;
                foreach (var item in resultsArray.EnumerateArray())
                {
                    var success = item.TryGetProperty("success", out var sv) && sv.GetBoolean();
                    var gibId = item.TryGetProperty("gibInvoiceId", out var g) ? g.GetString() : null;
                    var error = item.TryGetProperty("errorMessage", out var ev) ? ev.GetString() : null;
                    var orderId = i < requestList.Count ? requestList[i].OrderId : Guid.Empty;
                    results.Add(new BulkInvoiceItemResult(orderId, success, gibId, error));
                    i++;
                }
            }

            var successCount = results.Count(r => r.Success);
            return new BulkInvoiceResult(requestList.Count, successCount, results.Count - successCount, results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HBFatura CreateBulkInvoice exception");
            var failResults = requestList.Select(r =>
                new BulkInvoiceItemResult(r.OrderId, false, null, ex.Message)).ToList();
            return new BulkInvoiceResult(requestList.Count, 0, requestList.Count, failResults);
        }
    }

    // ── IKontorCapable ───────────────────────────────────────────────────

    public async Task<KontorBalanceDto> GetKontorBalanceAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("HBFatura GetKontorBalance");

        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/invoice/api/v1/kontor/balance", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("HBFatura GetKontorBalance failed: {Status}", response.StatusCode);
                return new KontorBalanceDto(0, 0, null, ProviderName);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var remaining = root.TryGetProperty("remainingKontor", out var r) ? r.GetInt32() : 0;
            var total = root.TryGetProperty("totalKontor", out var t) ? t.GetInt32() : 0;
            DateTime? expiresAt = root.TryGetProperty("expiresAt", out var ea)
                && ea.ValueKind != JsonValueKind.Null
                ? ea.GetDateTime()
                : null;

            return new KontorBalanceDto(remaining, total, expiresAt, ProviderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HBFatura GetKontorBalance exception");
            return new KontorBalanceDto(0, 0, null, ProviderName);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "HBFaturaProvider is not configured. Call Configure(username, apiKey, baseUrl) first.");
    }

    private static object BuildInvoicePayload(InvoiceDto invoice, string invoiceType)
    {
        return new
        {
            invoiceType,
            invoiceNumber = invoice.InvoiceNumber,
            customer = new
            {
                name = invoice.CustomerName,
                taxNumber = invoice.CustomerTaxNumber,
                taxOffice = invoice.CustomerTaxOffice,
                address = invoice.CustomerAddress
            },
            amounts = new
            {
                subTotal = invoice.SubTotal,
                taxTotal = invoice.TaxTotal,
                grandTotal = invoice.GrandTotal
            },
            lines = invoice.Lines.Select(l => new
            {
                productName = l.ProductName,
                sku = l.SKU,
                quantity = l.Quantity,
                unitPrice = l.UnitPrice,
                taxRate = l.TaxRate,
                taxAmount = l.TaxAmount,
                lineTotal = l.LineTotal
            }).ToArray()
        };
    }

    private static object BuildDispatchPayload(InvoiceDto invoice)
    {
        return new
        {
            dispatchType = "SEVK",
            dispatchNumber = invoice.InvoiceNumber,
            receiver = new
            {
                name = invoice.CustomerName,
                taxNumber = invoice.CustomerTaxNumber,
                taxOffice = invoice.CustomerTaxOffice,
                address = invoice.CustomerAddress
            },
            lines = invoice.Lines.Select(l => new
            {
                productName = l.ProductName,
                sku = l.SKU,
                quantity = l.Quantity,
                unitPrice = l.UnitPrice
            }).ToArray()
        };
    }

    private async Task<InvoiceResult> PostInvoiceAsync(string url, object payload, CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, CamelCaseOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("HBFatura POST {Url} failed: {Status} — {Error}",
                    url, response.StatusCode, errorBody);
                return new InvoiceResult(false, null, null, errorBody);
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var gibId = root.TryGetProperty("gibInvoiceId", out var gib) ? gib.GetString() : null;
            var pdfUrl = root.TryGetProperty("pdfUrl", out var pdf) ? pdf.GetString() : null;

            return new InvoiceResult(true, gibId, pdfUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HBFatura POST {Url} exception", url);
            return new InvoiceResult(false, null, null, ex.Message);
        }
    }
}
