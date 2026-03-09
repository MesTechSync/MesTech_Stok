using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// Sovos e-Fatura entegrasyonu — REST JSON API.
/// Sovos otomatik UBL olusturur, biz JSON gonderiyoruz.
/// Desteklenen islemler: e-Fatura, e-Arsiv, e-Irsaliye, durum sorgulama, PDF, iptal.
/// </summary>
public class SovosInvoiceProvider : IInvoiceProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SovosInvoiceProvider> _logger;
    private string? _apiKey;
    private string? _baseUrl;
    private bool _isConfigured;

    public string ProviderName => "Sovos e-Fatura";
    public InvoiceProvider Provider => InvoiceProvider.Sovos;

    public SovosInvoiceProvider(HttpClient httpClient, ILogger<SovosInvoiceProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// API Key + Base URL ile provider'i yapilandirir.
    /// </summary>
    public void Configure(string apiKey, string baseUrl)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        _isConfigured = true;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);

        _logger.LogInformation("SovosInvoiceProvider configured for {BaseUrl}", _baseUrl);
    }

    public async Task<InvoiceResult> CreateEFaturaAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos CreateEFatura for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildInvoicePayload(invoice, "SATIS");
        return await PostInvoiceAsync($"{_baseUrl}/api/invoices/outgoing", payload, ct);
    }

    public async Task<InvoiceResult> CreateEArsivAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos CreateEArsiv for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildInvoicePayload(invoice, "EARSIV");
        return await PostInvoiceAsync($"{_baseUrl}/api/invoices/outgoing", payload, ct);
    }

    public async Task<InvoiceResult> CreateEIrsaliyeAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos CreateEIrsaliye for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildDispatchPayload(invoice);
        return await PostInvoiceAsync($"{_baseUrl}/api/dispatches/outgoing", payload, ct);
    }

    public async Task<InvoiceStatusResult> CheckStatusAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos CheckStatus for {GibInvoiceId}", gibInvoiceId);

        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/invoices/{gibInvoiceId}/status", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Sovos CheckStatus failed: {Status} — {Error}",
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
            _logger.LogError(ex, "Sovos CheckStatus exception for {GibInvoiceId}", gibInvoiceId);
            return new InvoiceStatusResult(gibInvoiceId, "Error", null, ex.Message);
        }
    }

    public async Task<byte[]> GetPdfAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos GetPdf for {GibInvoiceId}", gibInvoiceId);

        var response = await _httpClient.GetAsync(
            $"{_baseUrl}/api/invoices/{gibInvoiceId}/pdf", ct);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<bool> IsEInvoiceTaxpayerAsync(string taxNumber, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos IsEInvoiceTaxpayer check for {TaxNumber}", taxNumber);

        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/taxpayers/{taxNumber}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sovos taxpayer check returned {Status} for {TaxNumber}",
                    response.StatusCode, taxNumber);
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            // Sovos returns isRegistered flag
            return doc.RootElement.TryGetProperty("isRegistered", out var reg) && reg.GetBoolean();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sovos taxpayer check exception for {TaxNumber}", taxNumber);
            return false;
        }
    }

    public async Task<InvoiceResult> CancelInvoiceAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos CancelInvoice for {GibInvoiceId}", gibInvoiceId);

        try
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/api/invoices/{gibInvoiceId}/cancel", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Sovos CancelInvoice failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return new InvoiceResult(false, gibInvoiceId, null, errorBody);
            }

            return new InvoiceResult(true, gibInvoiceId, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sovos CancelInvoice exception for {GibInvoiceId}", gibInvoiceId);
            return new InvoiceResult(false, gibInvoiceId, null, ex.Message);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "SovosInvoiceProvider is not configured. Call Configure(apiKey, baseUrl) first.");
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
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Sovos POST {Url} failed: {Status} — {Error}",
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
            _logger.LogError(ex, "Sovos POST {Url} exception", url);
            return new InvoiceResult(false, null, null, ex.Message);
        }
    }
}
