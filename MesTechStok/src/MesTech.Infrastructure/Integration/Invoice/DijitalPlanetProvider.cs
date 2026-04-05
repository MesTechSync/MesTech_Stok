using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Security;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// Dijital Planet e-Fatura entegrasyonu — pure REST JSON API.
/// Bearer token auth, temel IInvoiceProvider islemleri.
/// Desteklenen islemler: e-Fatura, e-Arsiv, e-Irsaliye, durum sorgulama, PDF, iptal.
/// URL pattern: {baseUrl}/api/invoices/...
/// </summary>
public sealed class DijitalPlanetProvider : IInvoiceProvider, IEInvoiceProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DijitalPlanetProvider> _logger;
    private string? _bearerToken;
    private string? _baseUrl;
    private bool _isConfigured;

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string ProviderName => "Dijital Planet";
    public InvoiceProvider Provider => InvoiceProvider.DijitalPlanet;

    public DijitalPlanetProvider(HttpClient httpClient, ILogger<DijitalPlanetProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Bearer token + Base URL ile provider'i yapilandirir.
    /// </summary>
    public void Configure(string bearerToken, string baseUrl)
    {
        _bearerToken = bearerToken ?? throw new ArgumentNullException(nameof(bearerToken));
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        _isConfigured = true;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _bearerToken);

        _logger.LogInformation("DijitalPlanetProvider configured for {BaseUrl}", _baseUrl);
    }

    // ── IInvoiceProvider ─────────────────────────────────────────────────

    public async Task<InvoiceResult> CreateEFaturaAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("DijitalPlanet CreateEFatura for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildInvoicePayload(invoice, "EFATURA");
        return await PostInvoiceAsync($"{_baseUrl}/api/invoices/efatura", payload, ct).ConfigureAwait(false);
    }

    public async Task<InvoiceResult> CreateEArsivAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("DijitalPlanet CreateEArsiv for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildInvoicePayload(invoice, "EARSIV");
        return await PostInvoiceAsync($"{_baseUrl}/api/invoices/earsiv", payload, ct).ConfigureAwait(false);
    }

    public async Task<InvoiceResult> CreateEIrsaliyeAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("DijitalPlanet CreateEIrsaliye for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildDispatchPayload(invoice);
        return await PostInvoiceAsync($"{_baseUrl}/api/invoices/eirsaliye", payload, ct).ConfigureAwait(false);
    }

    public async Task<InvoiceStatusResult> CheckStatusAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("DijitalPlanet CheckStatus for {GibInvoiceId}", gibInvoiceId);

        try
        {
            using var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/invoices/{gibInvoiceId}/status", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("DijitalPlanet CheckStatus failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return new InvoiceStatusResult(gibInvoiceId, "Error", null, errorBody);
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "Unknown" : "Unknown";
            DateTime? acceptedAt = root.TryGetProperty("acceptedAt", out var a) && a.ValueKind != JsonValueKind.Null
                ? a.GetDateTime()
                : null;
            var error = root.TryGetProperty("errorMessage", out var e) ? e.GetString() : null;

            return new InvoiceStatusResult(gibInvoiceId, status, acceptedAt, error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "DijitalPlanet CheckStatus exception for {GibInvoiceId}", gibInvoiceId);
            return new InvoiceStatusResult(gibInvoiceId, "Error", null, ex.Message);
        }
    }

    public async Task<byte[]> GetPdfAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("DijitalPlanet GetPdf for {GibInvoiceId}", gibInvoiceId);

        using var response = await _httpClient.GetAsync(
            $"{_baseUrl}/api/invoices/{gibInvoiceId}/pdf", ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> IsEInvoiceTaxpayerAsync(string taxNumber, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("DijitalPlanet IsEInvoiceTaxpayer check for {TaxNumber}", PiiLogMaskHelper.MaskTaxNumber(taxNumber));

        try
        {
            using var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/taxpayers/{taxNumber}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("DijitalPlanet taxpayer check returned {Status} for {TaxNumber}",
                    response.StatusCode, PiiLogMaskHelper.MaskTaxNumber(taxNumber));
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.TryGetProperty("isRegistered", out var reg) && reg.GetBoolean();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "DijitalPlanet taxpayer check exception for {TaxNumber}", PiiLogMaskHelper.MaskTaxNumber(taxNumber));
            return false;
        }
    }

    public async Task<InvoiceResult> CancelInvoiceAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("DijitalPlanet CancelInvoice for {GibInvoiceId}", gibInvoiceId);

        try
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(
                $"{_baseUrl}/api/invoices/{gibInvoiceId}/cancel", content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("DijitalPlanet CancelInvoice failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return new InvoiceResult(false, gibInvoiceId, null, errorBody);
            }

            return new InvoiceResult(true, gibInvoiceId, null, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "DijitalPlanet CancelInvoice exception for {GibInvoiceId}", gibInvoiceId);
            return new InvoiceResult(false, gibInvoiceId, null, ex.Message);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "DijitalPlanetProvider is not configured. Call Configure(bearerToken, baseUrl) first.");
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
        const int maxRetries = 3;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload, CamelCaseOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(url, content, ct).ConfigureAwait(false);

                if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                {
                    var retryBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogWarning("DijitalPlanet POST {Url} retry {Attempt}/{Max}: {Status} — {Error}",
                        url, attempt, maxRetries, response.StatusCode, retryBody);
                    if (attempt < maxRetries) { await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct).ConfigureAwait(false); continue; }
                    return new InvoiceResult(false, null, null, $"HTTP {(int)response.StatusCode} after {maxRetries} retries: {retryBody}");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogWarning("DijitalPlanet POST {Url} failed: {Status} — {Error}",
                        url, response.StatusCode, errorBody);
                    return new InvoiceResult(false, null, null, errorBody);
                }

                var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                var gibId = root.TryGetProperty("gibInvoiceId", out var gib) ? gib.GetString() : null;
                var pdfUrl = root.TryGetProperty("pdfUrl", out var pdf) ? pdf.GetString() : null;

                return new InvoiceResult(true, gibId, pdfUrl, null);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "DijitalPlanet POST {Url} network retry {Attempt}/{Max}", url, attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "DijitalPlanet POST {Url} exception", url);
                return new InvoiceResult(false, null, null, ex.Message);
            }
        }
        return new InvoiceResult(false, null, null, "Max retries exhausted");
    }

    // ── IEInvoiceProvider ────────────────────────────────────────────────

    string IEInvoiceProvider.ProviderCode => "DijitalPlanet";

    async Task<EInvoiceSendResult> IEInvoiceProvider.SendAsync(EInvoiceDocument document, CancellationToken ct)
    {
        EnsureConfigured();
        _logger.LogInformation("[DijitalPlanet] IEInvoiceProvider.SendAsync ETTN={Ettn}", document.EttnNo);
        try
        {
            var payload = new
            {
                ettnNo = document.EttnNo,
                gibUuid = document.GibUuid,
                scenario = document.Scenario.ToString(),
                buyerVkn = document.BuyerVkn,
                buyerTitle = document.BuyerTitle,
                issueDate = document.IssueDate.ToString("yyyy-MM-dd")
            };
            var json = JsonSerializer.Serialize(payload, CamelCaseOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync($"{_baseUrl}/api/einvoice/send", content, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return new EInvoiceSendResult(false, null, err, 0);
            }
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var providerRef = root.TryGetProperty("invoiceId", out var iid) ? iid.GetString()
                            : root.TryGetProperty("uuid", out var uid) ? uid.GetString() : null;
            return new EInvoiceSendResult(true, providerRef, null, 1);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[DijitalPlanet] IEInvoiceProvider.SendAsync exception");
            return new EInvoiceSendResult(false, null, ex.Message, 0);
        }
    }

    async Task<string?> IEInvoiceProvider.GetPdfUrlAsync(string providerRef, CancellationToken ct)
    {
        EnsureConfigured();
        try
        {
            using var response = await _httpClient.GetAsync($"{_baseUrl}/api/invoices/{providerRef}/pdf", ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("pdfUrl", out var urlEl) ? urlEl.GetString() : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[DijitalPlanet] GetPdfUrlAsync exception ref={Ref}", providerRef);
            return null;
        }
    }

    async Task<bool> IEInvoiceProvider.CancelAsync(string providerRef, string reason, CancellationToken ct)
    {
        EnsureConfigured();
        try
        {
            var payload = new { reason };
            var json = JsonSerializer.Serialize(payload, CamelCaseOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync($"{_baseUrl}/api/invoices/{providerRef}/cancel", content, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[DijitalPlanet] CancelAsync exception ref={Ref}", providerRef);
            return false;
        }
    }

    async Task<VknMukellefResult> IEInvoiceProvider.CheckVknMukellefAsync(string vkn, CancellationToken ct)
    {
        EnsureConfigured();
        try
        {
            using var response = await _httpClient.GetAsync($"{_baseUrl}/api/taxpayer/{vkn}", ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new VknMukellefResult(vkn, false, false, null, DateTime.UtcNow);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var isEInvoice = root.TryGetProperty("isEInvoice", out var ei) && ei.GetBoolean();
            var isEArchive = root.TryGetProperty("isEArchive", out var ea) && ea.GetBoolean();
            var title = root.TryGetProperty("title", out var tv) ? tv.GetString() : null;
            return new VknMukellefResult(vkn, isEInvoice, isEArchive, title, DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[DijitalPlanet] CheckVknMukellefAsync exception vkn={Vkn}", vkn);
            return new VknMukellefResult(vkn, false, false, null, DateTime.UtcNow);
        }
    }

    async Task<int> IEInvoiceProvider.GetCreditBalanceAsync(CancellationToken ct)
    {
        EnsureConfigured();
        try
        {
            using var response = await _httpClient.GetAsync($"{_baseUrl}/api/account/balance", ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return -1;
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("balance", out var b) ? b.GetInt32()
                 : doc.RootElement.TryGetProperty("credits", out var c) ? c.GetInt32() : -1;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[DijitalPlanet] GetCreditBalanceAsync exception");
            return -1;
        }
    }
}
