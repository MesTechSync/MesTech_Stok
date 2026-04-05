using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Auth;
using MesTech.Infrastructure.Security;
using Microsoft.Extensions.Logging;
namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// Parasut e-Fatura entegrasyonu — OAuth 2.0 + JSON:API format.
/// Content-Type: application/vnd.api+json
/// Request/response wrapped in { data: { type, attributes } }
/// </summary>
public sealed class ParasutInvoiceProvider : IInvoiceProvider, IBulkInvoiceCapable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ParasutInvoiceProvider> _logger;
    private string? _companyId;
    private string? _baseUrl;
    private OAuth2AuthProvider? _authProvider;
    private bool _isConfigured;

    public string ProviderName => "Parasut e-Fatura";
    public InvoiceProvider Provider => InvoiceProvider.Parasut;

    private static readonly JsonSerializerOptions s_snakeCaseJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private static readonly MediaTypeHeaderValue JsonApiMediaType =
        new("application/vnd.api+json");

    public ParasutInvoiceProvider(HttpClient httpClient, ILogger<ParasutInvoiceProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// OAuth2 auth provider + company ID ile yapilandirir.
    /// </summary>
    public void Configure(string companyId, OAuth2AuthProvider authProvider, string baseUrl)
    {
        _companyId = companyId ?? throw new ArgumentNullException(nameof(companyId));
        _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        _isConfigured = true;

        _logger.LogInformation("ParasutInvoiceProvider configured for company {CompanyId} at {BaseUrl}",
            _companyId, _baseUrl);
    }

    public async Task<InvoiceResult> CreateEFaturaAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Parasut CreateEFatura for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildEInvoicePayload(invoice, "e_invoice");
        return await PostJsonApiAsync($"{_baseUrl}/v4/{_companyId}/e_invoices", payload, ct).ConfigureAwait(false);
    }

    public async Task<InvoiceResult> CreateEArsivAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Parasut CreateEArsiv for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildEArchivePayload(invoice);
        return await PostJsonApiAsync($"{_baseUrl}/v4/{_companyId}/e_archives", payload, ct).ConfigureAwait(false);
    }

    public async Task<InvoiceResult> CreateEIrsaliyeAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Parasut CreateEIrsaliye for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildEInvoicePayload(invoice, "e_dispatch");
        return await PostJsonApiAsync($"{_baseUrl}/v4/{_companyId}/e_invoices", payload, ct).ConfigureAwait(false);
    }

    public async Task<InvoiceStatusResult> CheckStatusAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Parasut CheckStatus for {GibInvoiceId}", gibInvoiceId);

        try
        {
            await SetAuthHeaderAsync(ct).ConfigureAwait(false);

            using var response = await _httpClient.GetAsync(
                $"{_baseUrl}/v4/{_companyId}/e_invoices/{gibInvoiceId}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Parasut CheckStatus failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return new InvoiceStatusResult(gibInvoiceId, "Error", null, errorBody);
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            // JSON:API format: { data: { attributes: { status, ... } } }
            if (!doc.RootElement.TryGetProperty("data", out var dataEl)
                || !dataEl.TryGetProperty("attributes", out var attributes))
            {
                _logger.LogWarning("[ParasutInvoice] Unexpected JSON:API response — missing data.attributes");
                return new InvoiceStatusResult(gibInvoiceId, "Unknown", null, "Unexpected API response");
            }

            var status = attributes.TryGetProperty("status", out var s)
                ? s.GetString() ?? "Unknown" : "Unknown";
            DateTime? acceptedAt = attributes.TryGetProperty("accepted_at", out var a) && a.ValueKind != JsonValueKind.Null
                ? a.GetDateTime()
                : null;
            var error = attributes.TryGetProperty("error_message", out var e) ? e.GetString() : null;

            return new InvoiceStatusResult(gibInvoiceId, status, acceptedAt, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parasut CheckStatus exception for {GibInvoiceId}", gibInvoiceId);
            return new InvoiceStatusResult(gibInvoiceId, "Error", null, ex.Message);
        }
    }

    public async Task<byte[]> GetPdfAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Parasut GetPdf for {GibInvoiceId}", gibInvoiceId);

        await SetAuthHeaderAsync(ct).ConfigureAwait(false);

        using var response = await _httpClient.GetAsync(
            $"{_baseUrl}/v4/{_companyId}/e_invoices/{gibInvoiceId}/pdf", ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> IsEInvoiceTaxpayerAsync(string taxNumber, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Parasut IsEInvoiceTaxpayer check for {TaxNumber}", PiiLogMaskHelper.MaskTaxNumber(taxNumber));

        try
        {
            await SetAuthHeaderAsync(ct).ConfigureAwait(false);

            using var response = await _httpClient.GetAsync(
                $"{_baseUrl}/v4/{_companyId}/e_invoice_inboxes?filter[vkn]={taxNumber}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Parasut taxpayer check returned {Status} for {TaxNumber}",
                    response.StatusCode, PiiLogMaskHelper.MaskTaxNumber(taxNumber));
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            // JSON:API: { data: [...] } — non-empty data means taxpayer is registered
            if (!doc.RootElement.TryGetProperty("data", out var data))
                return false;
            return data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parasut taxpayer check exception for {TaxNumber}", PiiLogMaskHelper.MaskTaxNumber(taxNumber));
            return false;
        }
    }

    public async Task<InvoiceResult> CancelInvoiceAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Parasut CancelInvoice for {GibInvoiceId}", gibInvoiceId);

        try
        {
            await SetAuthHeaderAsync(ct).ConfigureAwait(false);

            var request = new HttpRequestMessage(HttpMethod.Delete,
                $"{_baseUrl}/v4/{_companyId}/e_invoices/{gibInvoiceId}");

            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Parasut CancelInvoice failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return new InvoiceResult(false, gibInvoiceId, null, errorBody);
            }

            return new InvoiceResult(true, gibInvoiceId, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parasut CancelInvoice exception for {GibInvoiceId}", gibInvoiceId);
            return new InvoiceResult(false, gibInvoiceId, null, ex.Message);
        }
    }

    // ── IBulkInvoiceCapable ────────────────────────────────────────────

    public async Task<BulkInvoiceResult> CreateBulkInvoiceAsync(
        IEnumerable<InvoiceCreateRequest> requests, CancellationToken ct = default)
    {
        EnsureConfigured();

        var requestList = requests.ToList();
        _logger.LogInformation("Parasut CreateBulkInvoice for {Count} invoices", requestList.Count);

        try
        {
            await SetAuthHeaderAsync(ct).ConfigureAwait(false);

            var dataArray = requestList.Select(req => new
            {
                type = "e_invoice",
                attributes = new
                {
                    invoice_number = req.PlatformOrderId,
                    customer_name = req.Customer.Name,
                    customer_tax_number = req.Customer.TaxNumber,
                    customer_tax_office = req.Customer.TaxOffice,
                    customer_address = req.Customer.Address,
                    grand_total = req.TotalAmount,
                    lines = req.Lines.Select(l => new
                    {
                        product_name = l.ProductName,
                        sku = l.SKU,
                        quantity = l.Quantity,
                        unit_price = l.UnitPrice,
                        tax_rate = l.TaxRate
                    }).ToArray()
                }
            }).ToArray();

            var payload = new { data = dataArray };

            var json = JsonSerializer.Serialize(payload, s_snakeCaseJson);
            var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = JsonApiMediaType;

            var url = $"{_baseUrl}/v4/{_companyId}/e_invoices/bulk";
            using var response = await _httpClient.PostAsync(url, content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Parasut bulk POST failed: {Status} — {Error}",
                    response.StatusCode, errorBody);

                var failResults = requestList.Select(r =>
                    new BulkInvoiceItemResult(r.OrderId, false, null, errorBody)).ToList();

                return new BulkInvoiceResult(requestList.Count, 0, requestList.Count, failResults);
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);

            if (!doc.RootElement.TryGetProperty("data", out var dataElement))
            {
                _logger.LogWarning("[ParasutInvoice] BulkCreate: unexpected response — missing data element");
                var failAll = requestList.Select(r =>
                    new BulkInvoiceItemResult(r.OrderId, false, null, "Unexpected API response")).ToList();
                return new BulkInvoiceResult(requestList.Count, 0, requestList.Count, failAll);
            }
            var results = new List<BulkInvoiceItemResult>();
            var i = 0;

            foreach (var item in dataElement.EnumerateArray())
            {
                if (!item.TryGetProperty("attributes", out var attrs))
                    continue;
                var gibId = attrs.TryGetProperty("gib_invoice_id", out var gib)
                    && gib.ValueKind != JsonValueKind.Null
                    ? gib.GetString() : null;

                var success = !string.IsNullOrEmpty(gibId);
                var orderId = i < requestList.Count ? requestList[i].OrderId : Guid.NewGuid();
                results.Add(new BulkInvoiceItemResult(orderId, success, gibId,
                    success ? null : "Missing gib_invoice_id in response"));
                i++;
            }

            var successCount = results.Count(r => r.Success);
            return new BulkInvoiceResult(requestList.Count, successCount, results.Count - successCount, results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parasut CreateBulkInvoice exception");

            var failResults = requestList.Select(r =>
                new BulkInvoiceItemResult(r.OrderId, false, null, ex.Message)).ToList();

            return new BulkInvoiceResult(requestList.Count, 0, requestList.Count, failResults);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "ParasutInvoiceProvider is not configured. Call Configure(companyId, authProvider, baseUrl) first.");
    }

    private async Task SetAuthHeaderAsync(CancellationToken ct)
    {
        var token = await _authProvider!.GetTokenAsync(ct).ConfigureAwait(false);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
    }

    private static object BuildEInvoicePayload(InvoiceDto invoice, string invoiceType)
    {
        return new
        {
            data = new
            {
                type = invoiceType,
                attributes = new
                {
                    invoice_number = invoice.InvoiceNumber,
                    customer_name = invoice.CustomerName,
                    customer_tax_number = invoice.CustomerTaxNumber,
                    customer_tax_office = invoice.CustomerTaxOffice,
                    customer_address = invoice.CustomerAddress,
                    sub_total = invoice.SubTotal,
                    tax_total = invoice.TaxTotal,
                    grand_total = invoice.GrandTotal,
                    lines = invoice.Lines.Select(l => new
                    {
                        product_name = l.ProductName,
                        sku = l.SKU,
                        quantity = l.Quantity,
                        unit_price = l.UnitPrice,
                        tax_rate = l.TaxRate,
                        tax_amount = l.TaxAmount,
                        line_total = l.LineTotal
                    }).ToArray()
                }
            }
        };
    }

    private static object BuildEArchivePayload(InvoiceDto invoice)
    {
        return new
        {
            data = new
            {
                type = "e_archives",
                attributes = new
                {
                    invoice_number = invoice.InvoiceNumber,
                    customer_name = invoice.CustomerName,
                    customer_tax_number = invoice.CustomerTaxNumber,
                    customer_tax_office = invoice.CustomerTaxOffice,
                    customer_address = invoice.CustomerAddress,
                    sub_total = invoice.SubTotal,
                    tax_total = invoice.TaxTotal,
                    grand_total = invoice.GrandTotal,
                    lines = invoice.Lines.Select(l => new
                    {
                        product_name = l.ProductName,
                        sku = l.SKU,
                        quantity = l.Quantity,
                        unit_price = l.UnitPrice,
                        tax_rate = l.TaxRate,
                        tax_amount = l.TaxAmount,
                        line_total = l.LineTotal
                    }).ToArray()
                }
            }
        };
    }

    private async Task<InvoiceResult> PostJsonApiAsync(string url, object payload, CancellationToken ct)
    {
        try
        {
            await SetAuthHeaderAsync(ct).ConfigureAwait(false);

            var json = JsonSerializer.Serialize(payload, s_snakeCaseJson);
            var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = JsonApiMediaType;

            using var response = await _httpClient.PostAsync(url, content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Parasut POST {Url} failed: {Status} — {Error}",
                    url, response.StatusCode, errorBody);
                return new InvoiceResult(false, null, null, errorBody);
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);

            // JSON:API: { data: { id, attributes: { gib_invoice_id, pdf_url } } }
            if (!doc.RootElement.TryGetProperty("data", out var data)
                || !data.TryGetProperty("attributes", out var attrs))
            {
                _logger.LogWarning("[ParasutInvoice] POST {Url}: unexpected response — missing data.attributes", url);
                return new InvoiceResult(false, null, null, "Unexpected API response format");
            }

            var gibId = attrs.TryGetProperty("gib_invoice_id", out var gib) ? gib.GetString() : null;
            var pdfUrl = attrs.TryGetProperty("pdf_url", out var pdf) ? pdf.GetString() : null;

            return new InvoiceResult(true, gibId, pdfUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parasut POST {Url} exception", url);
            return new InvoiceResult(false, null, null, ex.Message);
        }
    }
}
