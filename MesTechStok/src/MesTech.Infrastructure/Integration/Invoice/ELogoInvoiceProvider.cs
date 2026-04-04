using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Soap;
using MesTech.Infrastructure.Security;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// e-Logo e-Fatura entegrasyonu — REST+SOAP hybrid.
/// Auth ve sorgulama REST (Bearer token), fatura gonderim UBL-TR/SOAP (SimpleSoapClient).
/// Desteklenen islemler: e-Fatura, e-Arsiv, e-Irsaliye, durum, PDF, iptal,
/// toplu fatura, gelen fatura, kontor bakiye.
/// </summary>
public sealed class ELogoInvoiceProvider : IInvoiceProvider, IBulkInvoiceCapable, IIncomingInvoiceCapable, IKontorCapable
{
    private readonly HttpClient _httpClient;
    private readonly SimpleSoapClient _soapClient;
    private readonly ILogger<ELogoInvoiceProvider> _logger;
    private string? _apiKey;
    private string? _baseUrl;
    private bool _isConfigured;

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string ProviderName => "e-Logo";
    public InvoiceProvider Provider => InvoiceProvider.ELogo;

    public ELogoInvoiceProvider(HttpClient httpClient, SimpleSoapClient soapClient, ILogger<ELogoInvoiceProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _soapClient = soapClient ?? throw new ArgumentNullException(nameof(soapClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// API Key + Base URL ile provider'i yapilandirir.
    /// apiKey: Bearer token (REST), baseUrl: REST base URL.
    /// SOAP URL: {baseUrl}/soap/invoice.
    /// </summary>
    public void Configure(string apiKey, string baseUrl)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        _isConfigured = true;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);

        _logger.LogInformation("ELogoInvoiceProvider configured for {BaseUrl}", _baseUrl);
    }

    // ── IInvoiceProvider — SOAP methods ──────────────────────────────────

    public async Task<InvoiceResult> CreateEFaturaAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("e-Logo CreateEFatura for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var body = BuildUblInvoiceBody(invoice, "SATIS");
        return await SendSoapInvoiceAsync("createInvoice", body, ct).ConfigureAwait(false);
    }

    public async Task<InvoiceResult> CreateEArsivAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("e-Logo CreateEArsiv for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var body = BuildUblInvoiceBody(invoice, "EARSIV");
        return await SendSoapInvoiceAsync("createInvoice", body, ct).ConfigureAwait(false);
    }

    public async Task<InvoiceResult> CreateEIrsaliyeAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("e-Logo CreateEIrsaliye for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var body = BuildUblDispatchBody(invoice);
        return await SendSoapInvoiceAsync("createDispatch", body, ct).ConfigureAwait(false);
    }

    // ── IInvoiceProvider — REST methods ──────────────────────────────────

    public async Task<InvoiceStatusResult> CheckStatusAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("e-Logo CheckStatus for {GibInvoiceId}", gibInvoiceId);

        try
        {
            using var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/invoices/{gibInvoiceId}/status", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("e-Logo CheckStatus failed: {Status} — {Error}",
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "e-Logo CheckStatus exception for {GibInvoiceId}", gibInvoiceId);
            return new InvoiceStatusResult(gibInvoiceId, "Error", null, ex.Message);
        }
    }

    public async Task<byte[]> GetPdfAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("e-Logo GetPdf for {GibInvoiceId}", gibInvoiceId);

        using var response = await _httpClient.GetAsync(
            $"{_baseUrl}/api/invoices/{gibInvoiceId}/pdf", ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> IsEInvoiceTaxpayerAsync(string taxNumber, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("e-Logo IsEInvoiceTaxpayer check for {TaxNumber}", PiiLogMaskHelper.MaskTaxNumber(taxNumber));

        try
        {
            using var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/taxpayers/{taxNumber}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("e-Logo taxpayer check returned {Status} for {TaxNumber}",
                    response.StatusCode, PiiLogMaskHelper.MaskTaxNumber(taxNumber));
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.TryGetProperty("isRegistered", out var reg) && reg.GetBoolean();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "e-Logo taxpayer check exception for {TaxNumber}", PiiLogMaskHelper.MaskTaxNumber(taxNumber));
            return false;
        }
    }

    public async Task<InvoiceResult> CancelInvoiceAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("e-Logo CancelInvoice for {GibInvoiceId}", gibInvoiceId);

        try
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(
                $"{_baseUrl}/api/invoices/{gibInvoiceId}/cancel", content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("e-Logo CancelInvoice failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return new InvoiceResult(false, gibInvoiceId, null, errorBody);
            }

            return new InvoiceResult(true, gibInvoiceId, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "e-Logo CancelInvoice exception for {GibInvoiceId}", gibInvoiceId);
            return new InvoiceResult(false, gibInvoiceId, null, ex.Message);
        }
    }

    // ── IBulkInvoiceCapable ──────────────────────────────────────────────

    public async Task<BulkInvoiceResult> CreateBulkInvoiceAsync(
        IEnumerable<InvoiceCreateRequest> requests, CancellationToken ct = default)
    {
        EnsureConfigured();
        var requestList = requests.ToList();
        _logger.LogInformation("e-Logo CreateBulkInvoice for {Count} invoices", requestList.Count);

        try
        {
            var payloads = requestList.Select(req => new
            {
                invoiceType = "SATIS",
                invoiceNumber = req.PlatformOrderId,
                customer = new
                {
                    name = req.Customer.Name,
                    taxNumber = req.Customer.TaxNumber,
                    taxOffice = req.Customer.TaxOffice,
                    address = req.Customer.Address
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
                    taxRate = l.TaxRate
                }).ToArray()
            }).ToArray();
            var payload = new { invoices = payloads };
            var json = JsonSerializer.Serialize(payload, CamelCaseOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(
                $"{_baseUrl}/api/invoices/outgoing/bulk", content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("e-Logo CreateBulkInvoice failed: {Status} — {Error}",
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "e-Logo CreateBulkInvoice exception");
            var failResults = requestList.Select(r =>
                new BulkInvoiceItemResult(r.OrderId, false, null, ex.Message)).ToList();
            return new BulkInvoiceResult(requestList.Count, 0, requestList.Count, failResults);
        }
    }

    // ── IIncomingInvoiceCapable ──────────────────────────────────────────

    public async Task<IReadOnlyList<IncomingInvoiceDto>> GetIncomingInvoicesAsync(
        DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("e-Logo GetIncomingInvoices from {From} to {To}",
            startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        try
        {
            var fromStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var toStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            using var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/invoices/incoming?from={fromStr}&to={toStr}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("e-Logo GetIncomingInvoices failed: {Status}", response.StatusCode);
                return Array.Empty<IncomingInvoiceDto>();
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var list = new List<IncomingInvoiceDto>();

            if (doc.RootElement.TryGetProperty("invoices", out var invoicesArray))
            {
                foreach (var item in invoicesArray.EnumerateArray())
                {
                    var gibId = item.TryGetProperty("gibInvoiceId", out var gibProp) ? gibProp.GetString() ?? string.Empty : string.Empty;
                    if (string.IsNullOrEmpty(gibId)) continue;
                    var invoiceNumber = item.TryGetProperty("invoiceNumber", out var inv)
                        ? inv.GetString() ?? gibId : gibId;
                    var senderName = item.TryGetProperty("senderName", out var snProp) ? snProp.GetString() ?? string.Empty : string.Empty;
                    var senderTax = item.TryGetProperty("senderTaxNumber", out var stProp) ? stProp.GetString() ?? string.Empty : string.Empty;
                    var amount = item.TryGetProperty("amount", out var amtProp) ? amtProp.GetDecimal() : 0m;
                    var invoiceDate = item.TryGetProperty("invoiceDate", out var dateProp) ? dateProp.GetDateTime() : DateTime.UtcNow;
                    var pdfUrl = item.TryGetProperty("pdfUrl", out var pdf)
                        && pdf.ValueKind != JsonValueKind.Null
                        ? pdf.GetString() : null;
                    var statusStr = item.TryGetProperty("status", out var statusProp) ? statusProp.GetString() ?? "Draft" : "Draft";
                    var status = Enum.TryParse<Domain.Enums.InvoiceStatus>(statusStr, true, out var parsed)
                        ? parsed : Domain.Enums.InvoiceStatus.Draft;
                    list.Add(new IncomingInvoiceDto(gibId, invoiceNumber, senderName, senderTax, amount, invoiceDate, pdfUrl, status));
                }
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "e-Logo GetIncomingInvoices exception");
            return Array.Empty<IncomingInvoiceDto>();
        }
    }

    public async Task<bool> AcceptInvoiceAsync(string invoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("e-Logo AcceptInvoice for {InvoiceId}", invoiceId);

        try
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(
                $"{_baseUrl}/api/invoices/incoming/{invoiceId}/accept", content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("e-Logo AcceptInvoice failed: {Status}", response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "e-Logo AcceptInvoice exception for {InvoiceId}", invoiceId);
            return false;
        }
    }

    public async Task<bool> RejectInvoiceAsync(string invoiceId, string reason, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("e-Logo RejectInvoice for {InvoiceId}", invoiceId);

        try
        {
            var payload = new { reason };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(
                $"{_baseUrl}/api/invoices/incoming/{invoiceId}/reject", content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("e-Logo RejectInvoice failed: {Status}", response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "e-Logo RejectInvoice exception for {InvoiceId}", invoiceId);
            return false;
        }
    }

    // ── IKontorCapable ──────────────────────────────────────────────────

    public async Task<KontorBalanceDto> GetKontorBalanceAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("e-Logo GetKontorBalance");

        try
        {
            using var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/account/kontor", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("e-Logo GetKontorBalance failed: {Status}", response.StatusCode);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "e-Logo GetKontorBalance exception");
            return new KontorBalanceDto(0, 0, null, ProviderName);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "ELogoInvoiceProvider is not configured. Call Configure(apiKey, baseUrl) first.");
    }

    private static XElement BuildUblInvoiceBody(InvoiceDto invoice, string invoiceType)
    {
        var ublNs = XNamespace.Get("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");
        var cacNs = XNamespace.Get("urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
        var cbcNs = XNamespace.Get("urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");

        return new XElement(ublNs + "Invoice",
            new XElement(cbcNs + "ID", invoice.InvoiceNumber),
            new XElement(cbcNs + "InvoiceTypeCode", invoiceType),
            new XElement(cacNs + "AccountingCustomerParty",
                new XElement(cacNs + "Party",
                    new XElement(cacNs + "PartyName",
                        new XElement(cbcNs + "Name", invoice.CustomerName)),
                    new XElement(cacNs + "PartyIdentification",
                        new XElement(cbcNs + "ID", invoice.CustomerTaxNumber ?? "")),
                    new XElement(cacNs + "PostalAddress",
                        new XElement(cbcNs + "StreetName", invoice.CustomerAddress)))),
            new XElement(cacNs + "LegalMonetaryTotal",
                new XElement(cbcNs + "TaxExclusiveAmount", invoice.SubTotal.ToString(CultureInfo.InvariantCulture)),
                new XElement(cbcNs + "TaxInclusiveAmount", invoice.GrandTotal.ToString(CultureInfo.InvariantCulture)),
                new XElement(cbcNs + "PayableAmount", invoice.GrandTotal.ToString(CultureInfo.InvariantCulture))),
            // Invoice lines
            invoice.Lines.Select((line, idx) =>
                new XElement(cacNs + "InvoiceLine",
                    new XElement(cbcNs + "ID", (idx + 1).ToString()),
                    new XElement(cbcNs + "InvoicedQuantity", line.Quantity.ToString()),
                    new XElement(cacNs + "Item",
                        new XElement(cbcNs + "Name", line.ProductName)),
                    new XElement(cacNs + "Price",
                        new XElement(cbcNs + "PriceAmount", line.UnitPrice.ToString(CultureInfo.InvariantCulture))))));
    }

    private static XElement BuildUblDispatchBody(InvoiceDto invoice)
    {
        var ublNs = XNamespace.Get("urn:oasis:names:specification:ubl:schema:xsd:DespatchAdvice-2");
        var cacNs = XNamespace.Get("urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
        var cbcNs = XNamespace.Get("urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");

        return new XElement(ublNs + "DespatchAdvice",
            new XElement(cbcNs + "ID", invoice.InvoiceNumber),
            new XElement(cbcNs + "DespatchAdviceTypeCode", "SEVK"),
            new XElement(cacNs + "DeliveryCustomerParty",
                new XElement(cacNs + "Party",
                    new XElement(cacNs + "PartyName",
                        new XElement(cbcNs + "Name", invoice.CustomerName)),
                    new XElement(cacNs + "PartyIdentification",
                        new XElement(cbcNs + "ID", invoice.CustomerTaxNumber ?? "")),
                    new XElement(cacNs + "PostalAddress",
                        new XElement(cbcNs + "StreetName", invoice.CustomerAddress)))),
            // Dispatch lines
            invoice.Lines.Select((line, idx) =>
                new XElement(cacNs + "DespatchLine",
                    new XElement(cbcNs + "ID", (idx + 1).ToString()),
                    new XElement(cbcNs + "DeliveredQuantity", line.Quantity.ToString()),
                    new XElement(cacNs + "Item",
                        new XElement(cbcNs + "Name", line.ProductName)))));
    }

    private async Task<InvoiceResult> SendSoapInvoiceAsync(string soapAction, XElement body, CancellationToken ct)
    {
        try
        {
            var soapUrl = $"{_baseUrl}/soap/invoice";
            var response = await _soapClient.SendAsync(soapUrl, soapAction, body, ct).ConfigureAwait(false);

            // Parse gibInvoiceId from response using namespace-agnostic local name matching
            var gibId = response.Descendants().FirstOrDefault(e => e.Name.LocalName == "gibInvoiceId")?.Value;
            var pdfUrl = response.Descendants().FirstOrDefault(e => e.Name.LocalName == "pdfUrl")?.Value;

            return new InvoiceResult(true, gibId, pdfUrl, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "e-Logo SOAP request failed for action {SoapAction}", soapAction);
            return new InvoiceResult(false, null, null, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "e-Logo SOAP fault for action {SoapAction}", soapAction);
            return new InvoiceResult(false, null, null, ex.Message);
        }
    }
}
