using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Security;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// Trendyol e-Faturam entegrasyonu — Trendyol partner API uzerinden e-Donusum.
/// Desteklenen islemler: e-Fatura, e-Arsiv, e-Irsaliye, durum sorgulama, PDF, iptal,
/// toplu fatura, kontor bakiye, sablon ayari.
/// Auth: Bearer token (Trendyol API key).
/// URL pattern: {baseUrl}/suppliers/{supplierId}/e-invoices/...
/// </summary>
public sealed class TrendyolEFaturamProvider : IInvoiceProvider, IBulkInvoiceCapable, IKontorCapable, IInvoiceTemplateCapable, IEInvoiceProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TrendyolEFaturamProvider> _logger;
    private string? _apiKey;
    private string? _baseUrl;
    private long _supplierId;
    private bool _isConfigured;

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string ProviderName => "Trendyol e-Faturam";
    public InvoiceProvider Provider => InvoiceProvider.TrendyolEFaturam;

    public TrendyolEFaturamProvider(HttpClient httpClient, ILogger<TrendyolEFaturamProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// API Key + Supplier ID + Base URL ile provider'i yapilandirir.
    /// </summary>
    public void Configure(string apiKey, long supplierId, string baseUrl)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        _supplierId = supplierId;
        _isConfigured = true;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);

        _logger.LogInformation(
            "TrendyolEFaturamProvider configured for SupplierId={SupplierId}, BaseUrl={BaseUrl}",
            _supplierId, _baseUrl);
    }

    // ── IInvoiceProvider ─────────────────────────────────────────────────

    public async Task<InvoiceResult> CreateEFaturaAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolEFaturam CreateEFatura for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildInvoicePayload(invoice, "EFATURA");
        return await PostInvoiceAsync($"{_baseUrl}/suppliers/{_supplierId}/e-invoices/efatura", payload, ct).ConfigureAwait(false);
    }

    public async Task<InvoiceResult> CreateEArsivAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolEFaturam CreateEArsiv for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildInvoicePayload(invoice, "EARSIV");
        return await PostInvoiceAsync($"{_baseUrl}/suppliers/{_supplierId}/e-invoices/earsiv", payload, ct).ConfigureAwait(false);
    }

    public async Task<InvoiceResult> CreateEIrsaliyeAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolEFaturam CreateEIrsaliye for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildDispatchPayload(invoice);
        return await PostInvoiceAsync($"{_baseUrl}/suppliers/{_supplierId}/e-invoices/eirsaliye", payload, ct).ConfigureAwait(false);
    }

    public async Task<InvoiceStatusResult> CheckStatusAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolEFaturam CheckStatus for {GibInvoiceId}", gibInvoiceId);

        try
        {
            using var response = await _httpClient.GetAsync(
                $"{_baseUrl}/suppliers/{_supplierId}/e-invoices/{gibInvoiceId}/status", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("TrendyolEFaturam CheckStatus failed: {Status} — {Error}",
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
            _logger.LogError(ex, "TrendyolEFaturam CheckStatus exception for {GibInvoiceId}", gibInvoiceId);
            return new InvoiceStatusResult(gibInvoiceId, "Error", null, ex.Message);
        }
    }

    public async Task<byte[]> GetPdfAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolEFaturam GetPdf for {GibInvoiceId}", gibInvoiceId);

        using var response = await _httpClient.GetAsync(
            $"{_baseUrl}/suppliers/{_supplierId}/e-invoices/{gibInvoiceId}/pdf", ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> IsEInvoiceTaxpayerAsync(string taxNumber, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolEFaturam IsEInvoiceTaxpayer check for {TaxNumber}", PiiLogMaskHelper.MaskTaxNumber(taxNumber));

        try
        {
            using var response = await _httpClient.GetAsync(
                $"{_baseUrl}/suppliers/{_supplierId}/e-invoices/taxpayer/{taxNumber}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TrendyolEFaturam taxpayer check returned {Status} for {TaxNumber}",
                    response.StatusCode, PiiLogMaskHelper.MaskTaxNumber(taxNumber));
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.TryGetProperty("isRegistered", out var reg) && reg.GetBoolean();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "TrendyolEFaturam taxpayer check exception for {TaxNumber}", PiiLogMaskHelper.MaskTaxNumber(taxNumber));
            return false;
        }
    }

    public async Task<InvoiceResult> CancelInvoiceAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolEFaturam CancelInvoice for {GibInvoiceId}", gibInvoiceId);

        try
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(
                $"{_baseUrl}/suppliers/{_supplierId}/e-invoices/{gibInvoiceId}/cancel", content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("TrendyolEFaturam CancelInvoice failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return new InvoiceResult(false, gibInvoiceId, null, errorBody);
            }

            return new InvoiceResult(true, gibInvoiceId, null, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "TrendyolEFaturam CancelInvoice exception for {GibInvoiceId}", gibInvoiceId);
            return new InvoiceResult(false, gibInvoiceId, null, ex.Message);
        }
    }

    // ── IBulkInvoiceCapable ──────────────────────────────────────────────

    public async Task<BulkInvoiceResult> CreateBulkInvoiceAsync(
        IEnumerable<InvoiceCreateRequest> requests, CancellationToken ct = default)
    {
        EnsureConfigured();
        var requestList = requests.ToList();
        _logger.LogInformation("TrendyolEFaturam CreateBulkInvoice for {Count} invoices", requestList.Count);

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
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(
                $"{_baseUrl}/suppliers/{_supplierId}/e-invoices/bulk", content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("TrendyolEFaturam CreateBulkInvoice failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                var failResults = requestList.Select(r =>
                    new BulkInvoiceItemResult(r.OrderId, false, null, errorBody)).ToList();
                return new BulkInvoiceResult(requestList.Count, 0, requestList.Count, failResults);
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
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
                    var orderId = i < requestList.Count ? requestList[i].OrderId : Guid.NewGuid();
                    results.Add(new BulkInvoiceItemResult(orderId, success, gibId, error));
                    i++;
                }
            }

            var successCount = results.Count(r => r.Success);
            return new BulkInvoiceResult(requestList.Count, successCount, results.Count - successCount, results);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "TrendyolEFaturam CreateBulkInvoice exception");
            var failResults = requestList.Select(r =>
                new BulkInvoiceItemResult(r.OrderId, false, null, ex.Message)).ToList();
            return new BulkInvoiceResult(requestList.Count, 0, requestList.Count, failResults);
        }
    }

    // ── IKontorCapable ───────────────────────────────────────────────────

    public async Task<KontorBalanceDto> GetKontorBalanceAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolEFaturam GetKontorBalance");

        try
        {
            using var response = await _httpClient.GetAsync(
                $"{_baseUrl}/suppliers/{_supplierId}/e-invoices/kontor", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TrendyolEFaturam GetKontorBalance failed: {Status}", response.StatusCode);
                return new KontorBalanceDto(0, 0, null, ProviderName);
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var remaining = root.TryGetProperty("remaining", out var r) ? r.GetInt32() : 0;
            var total = root.TryGetProperty("total", out var t) ? t.GetInt32() : 0;
            DateTime? expiresAt = root.TryGetProperty("expiresAt", out var ea)
                && ea.ValueKind != JsonValueKind.Null
                ? ea.GetDateTime()
                : null;

            return new KontorBalanceDto(remaining, total, expiresAt, ProviderName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "TrendyolEFaturam GetKontorBalance exception");
            return new KontorBalanceDto(0, 0, null, ProviderName);
        }
    }

    // ── IInvoiceTemplateCapable ──────────────────────────────────────────

    public async Task<bool> SetInvoiceTemplateAsync(InvoiceTemplateDto template, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolEFaturam SetInvoiceTemplate");

        try
        {
            var payload = new
            {
                phone = template.PhoneNumber,
                email = template.Email,
                ticaretSicilNo = template.TicaretSicilNo,
                showKargoBarkodu = template.ShowKargoBarkodu,
                showTutarYaziyla = template.ShowFaturaTutariYaziyla,
                defaultKdv = (int)template.DefaultKdv,
                logoBase64 = template.LogoImage != null ? Convert.ToBase64String(template.LogoImage.ToArray()) : null,
                signatureBase64 = template.SignatureImage != null ? Convert.ToBase64String(template.SignatureImage.ToArray()) : null
            };
            var json = JsonSerializer.Serialize(payload, CamelCaseOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PutAsync(
                $"{_baseUrl}/suppliers/{_supplierId}/e-invoices/template", content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TrendyolEFaturam SetInvoiceTemplate failed: {Status}", response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "TrendyolEFaturam SetInvoiceTemplate exception");
            return false;
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "TrendyolEFaturamProvider is not configured. Call Configure(apiKey, supplierId, baseUrl) first.");
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

                // 429 veya 5xx → retry
                if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                {
                    var retryBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogWarning(
                        "TrendyolEFaturam POST {Url} retry {Attempt}/{Max}: {Status} — {Error}",
                        url, attempt, maxRetries, response.StatusCode, retryBody);

                    if (attempt < maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2s, 4s, 8s
                        await Task.Delay(delay, ct).ConfigureAwait(false);
                        continue;
                    }

                    return new InvoiceResult(false, null, null, $"HTTP {(int)response.StatusCode} after {maxRetries} retries: {retryBody}");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogWarning("TrendyolEFaturam POST {Url} failed: {Status} — {Error}",
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
                _logger.LogWarning(ex, "TrendyolEFaturam POST {Url} network retry {Attempt}/{Max}", url, attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "TrendyolEFaturam POST {Url} exception", url);
                return new InvoiceResult(false, null, null, ex.Message);
            }
        }

        return new InvoiceResult(false, null, null, "Max retries exhausted");
    }

    // ── IEInvoiceProvider ────────────────────────────────────────────────

    string IEInvoiceProvider.ProviderCode => "TrendyolEFaturam";

    async Task<EInvoiceSendResult> IEInvoiceProvider.SendAsync(EInvoiceDocument document, CancellationToken ct)
    {
        EnsureConfigured();
        _logger.LogInformation("[TY-EFaturam] IEInvoiceProvider.SendAsync ETTN={Ettn}", document.EttnNo);
        try
        {
            var url = $"{_baseUrl}/suppliers/{_supplierId}/e-invoices";
            var payload = new
            {
                ettnNo = document.EttnNo,
                gibUuid = document.GibUuid,
                scenario = document.Scenario.ToString(),
                receiverVkn = document.BuyerVkn,
                receiverTitle = document.BuyerTitle,
                issueDate = document.IssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
            var json = JsonSerializer.Serialize(payload, CamelCaseOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(url, content, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return new EInvoiceSendResult(false, null, err, 0);
            }
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var providerRef = root.TryGetProperty("eInvoiceId", out var eid) ? eid.GetString()
                            : root.TryGetProperty("providerRef", out var pr) ? pr.GetString() : null;
            return new EInvoiceSendResult(true, providerRef, null, 1);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[TY-EFaturam] IEInvoiceProvider.SendAsync exception");
            return new EInvoiceSendResult(false, null, ex.Message, 0);
        }
    }

    async Task<string?> IEInvoiceProvider.GetPdfUrlAsync(string providerRef, CancellationToken ct)
    {
        EnsureConfigured();
        try
        {
            var url = $"{_baseUrl}/suppliers/{_supplierId}/e-invoices/{providerRef}/pdf";
            using var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("pdfUrl", out var urlEl) ? urlEl.GetString() : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[TY-EFaturam] GetPdfUrlAsync exception ref={Ref}", providerRef);
            return null;
        }
    }

    async Task<bool> IEInvoiceProvider.CancelAsync(string providerRef, string reason, CancellationToken ct)
    {
        EnsureConfigured();
        try
        {
            var url = $"{_baseUrl}/suppliers/{_supplierId}/e-invoices/{providerRef}/cancel";
            var payload = new { reason };
            var json = JsonSerializer.Serialize(payload, CamelCaseOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(url, content, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[TY-EFaturam] CancelAsync exception ref={Ref}", providerRef);
            return false;
        }
    }

    async Task<VknMukellefResult> IEInvoiceProvider.CheckVknMukellefAsync(string vkn, CancellationToken ct)
    {
        EnsureConfigured();
        try
        {
            var url = $"{_baseUrl}/suppliers/{_supplierId}/mukellef/{vkn}";
            using var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new VknMukellefResult(vkn, false, false, null, DateTime.UtcNow);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var isEInvoice = root.TryGetProperty("isEInvoiceMukellef", out var eiv) && eiv.GetBoolean();
            var isEArchive = root.TryGetProperty("isEArchiveMukellef", out var eav) && eav.GetBoolean();
            var title = root.TryGetProperty("title", out var tv) ? tv.GetString() : null;
            return new VknMukellefResult(vkn, isEInvoice, isEArchive, title, DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[TY-EFaturam] CheckVknMukellefAsync exception vkn={Vkn}", vkn);
            return new VknMukellefResult(vkn, false, false, null, DateTime.UtcNow);
        }
    }

    async Task<int> IEInvoiceProvider.GetCreditBalanceAsync(CancellationToken ct)
    {
        EnsureConfigured();
        try
        {
            var url = $"{_baseUrl}/suppliers/{_supplierId}/e-invoice-credits";
            using var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return -1;
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("remainingCredits", out var rc) ? rc.GetInt32()
                 : doc.RootElement.TryGetProperty("balance", out var bal) ? bal.GetInt32() : -1;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[TY-EFaturam] GetCreditBalanceAsync exception");
            return -1;
        }
    }
}
