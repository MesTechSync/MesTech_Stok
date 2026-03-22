using System.Text;
using System.Xml.Linq;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// GIB e-Arsiv Portal entegrasyonu — pure SOAP/XML, VKN+Password auth.
/// Turkiye Gelir Idaresi Baskanligi (GIB) ucretsiz e-fatura portali.
/// Auth: SOAP Header icinde &lt;ear:UserInfo&gt; blogu (VKN + Password).
/// Endpoint: {baseUrl}/earsiv-services/dispatch
/// Desteklenen islemler: e-Fatura, e-Arsiv, e-Irsaliye, durum sorgulama, PDF, VKN sorgu, iptal.
/// </summary>
public class GibPortalProvider : IInvoiceProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GibPortalProvider> _logger;

    private string? _vkn;
    private string? _password;
    private string? _baseUrl;
    private bool _isConfigured;

    private static readonly XNamespace SoapEnv = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace EarNs = "urn:earsiv:services";

    public string ProviderName => "GIB Portal";
    public InvoiceProvider Provider => InvoiceProvider.GibPortal;

    public GibPortalProvider(HttpClient httpClient, ILogger<GibPortalProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// VKN + Password + BaseUrl ile provider'i yapilandirir.
    /// </summary>
    public void Configure(string vkn, string password, string baseUrl)
    {
        _vkn = vkn ?? throw new ArgumentNullException(nameof(vkn));
        _password = password ?? throw new ArgumentNullException(nameof(password));
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        _isConfigured = true;

        _logger.LogInformation("GibPortalProvider configured for {BaseUrl} with VKN {Vkn}", _baseUrl, _vkn);
    }

    // ── IInvoiceProvider ─────────────────────────────────────────────────

    public async Task<InvoiceResult> CreateEFaturaAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("GibPortal CreateEFatura for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        try
        {
            var bodyContent = new XElement(EarNs + "createInvoiceRequest",
                new XElement(EarNs + "invoiceType", "EFATURA"),
                new XElement(EarNs + "invoiceNumber", invoice.InvoiceNumber),
                new XElement(EarNs + "customerName", invoice.CustomerName),
                new XElement(EarNs + "customerTaxNumber", invoice.CustomerTaxNumber ?? string.Empty),
                new XElement(EarNs + "customerTaxOffice", invoice.CustomerTaxOffice ?? string.Empty),
                new XElement(EarNs + "customerAddress", invoice.CustomerAddress),
                new XElement(EarNs + "subTotal", invoice.SubTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement(EarNs + "taxTotal", invoice.TaxTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement(EarNs + "grandTotal", invoice.GrandTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                BuildInvoiceLines(invoice.Lines));

            var response = await SendSoapAsync("createInvoice", bodyContent, ct).ConfigureAwait(false);

            var gibId = response.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "gibInvoiceId")?.Value;
            var success = response.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "success")?.Value;

            if (success?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
                return new InvoiceResult(true, gibId, null, null);

            var errMsg = response.Descendants()
                             .FirstOrDefault(e => e.Name.LocalName == "errorMessage")?.Value
                         ?? "Unknown error from GIB Portal";
            return new InvoiceResult(false, null, null, errMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GibPortal CreateEFatura exception for {InvoiceNumber}", invoice.InvoiceNumber);
            return new InvoiceResult(false, null, null, ex.Message);
        }
    }

    public async Task<InvoiceResult> CreateEArsivAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("GibPortal CreateEArsiv for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        try
        {
            var bodyContent = new XElement(EarNs + "createInvoiceRequest",
                new XElement(EarNs + "invoiceType", "EARSIV"),
                new XElement(EarNs + "invoiceNumber", invoice.InvoiceNumber),
                new XElement(EarNs + "customerName", invoice.CustomerName),
                new XElement(EarNs + "customerTaxNumber", invoice.CustomerTaxNumber ?? string.Empty),
                new XElement(EarNs + "customerTaxOffice", invoice.CustomerTaxOffice ?? string.Empty),
                new XElement(EarNs + "customerAddress", invoice.CustomerAddress),
                new XElement(EarNs + "subTotal", invoice.SubTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement(EarNs + "taxTotal", invoice.TaxTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement(EarNs + "grandTotal", invoice.GrandTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                BuildInvoiceLines(invoice.Lines));

            var response = await SendSoapAsync("createInvoice", bodyContent, ct).ConfigureAwait(false);

            var gibId = response.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "gibInvoiceId")?.Value;
            var success = response.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "success")?.Value;

            if (success?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
                return new InvoiceResult(true, gibId, null, null);

            var errMsg = response.Descendants()
                             .FirstOrDefault(e => e.Name.LocalName == "errorMessage")?.Value
                         ?? "Unknown error from GIB Portal";
            return new InvoiceResult(false, null, null, errMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GibPortal CreateEArsiv exception for {InvoiceNumber}", invoice.InvoiceNumber);
            return new InvoiceResult(false, null, null, ex.Message);
        }
    }

    public async Task<InvoiceResult> CreateEIrsaliyeAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("GibPortal CreateEIrsaliye for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        try
        {
            var bodyContent = new XElement(EarNs + "createDispatchRequest",
                new XElement(EarNs + "dispatchType", "SEVK"),
                new XElement(EarNs + "dispatchNumber", invoice.InvoiceNumber),
                new XElement(EarNs + "receiverName", invoice.CustomerName),
                new XElement(EarNs + "receiverTaxNumber", invoice.CustomerTaxNumber ?? string.Empty),
                new XElement(EarNs + "receiverTaxOffice", invoice.CustomerTaxOffice ?? string.Empty),
                new XElement(EarNs + "receiverAddress", invoice.CustomerAddress),
                BuildDispatchLines(invoice.Lines));

            var response = await SendSoapAsync("createDispatch", bodyContent, ct).ConfigureAwait(false);

            var gibId = response.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "gibInvoiceId")?.Value;
            var success = response.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "success")?.Value;

            if (success?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
                return new InvoiceResult(true, gibId, null, null);

            var errMsg = response.Descendants()
                             .FirstOrDefault(e => e.Name.LocalName == "errorMessage")?.Value
                         ?? "Unknown error from GIB Portal";
            return new InvoiceResult(false, null, null, errMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GibPortal CreateEIrsaliye exception for {InvoiceNumber}", invoice.InvoiceNumber);
            return new InvoiceResult(false, null, null, ex.Message);
        }
    }

    public async Task<InvoiceStatusResult> CheckStatusAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("GibPortal CheckStatus for {GibInvoiceId}", gibInvoiceId);

        try
        {
            var bodyContent = new XElement(EarNs + "getInvoiceStatusRequest",
                new XElement(EarNs + "gibInvoiceId", gibInvoiceId));

            var response = await SendSoapAsync("getInvoiceStatus", bodyContent, ct).ConfigureAwait(false);

            var status = response.Descendants()
                             .FirstOrDefault(e => e.Name.LocalName == "status")?.Value
                         ?? "Unknown";

            DateTime? acceptedAt = null;
            var acceptedAtStr = response.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "acceptedAt")?.Value;
            if (!string.IsNullOrWhiteSpace(acceptedAtStr)
                && DateTime.TryParse(acceptedAtStr, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var parsedDate))
            {
                acceptedAt = parsedDate;
            }

            var errorMessage = response.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "errorMessage")?.Value;

            return new InvoiceStatusResult(gibInvoiceId, status, acceptedAt, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GibPortal CheckStatus exception for {GibInvoiceId}", gibInvoiceId);
            return new InvoiceStatusResult(gibInvoiceId, "Error", null, ex.Message);
        }
    }

    public async Task<byte[]> GetPdfAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("GibPortal GetPdf for {GibInvoiceId}", gibInvoiceId);

        var bodyContent = new XElement(EarNs + "getInvoicePdfRequest",
            new XElement(EarNs + "gibInvoiceId", gibInvoiceId));

        var response = await SendSoapAsync("getInvoicePdf", bodyContent, ct).ConfigureAwait(false);

        var pdfBase64 = response.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "pdfBase64")?.Value;

        if (string.IsNullOrWhiteSpace(pdfBase64))
        {
            _logger.LogWarning("GibPortal GetPdf returned empty pdfBase64 for {GibInvoiceId}", gibInvoiceId);
            throw new InvalidOperationException($"GIB Portal returned no PDF data for invoice {gibInvoiceId}");
        }

        return Convert.FromBase64String(pdfBase64);
    }

    public async Task<bool> IsEInvoiceTaxpayerAsync(string taxNumber, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("GibPortal IsEInvoiceTaxpayer check for {TaxNumber}", taxNumber);

        try
        {
            var bodyContent = new XElement(EarNs + "checkTaxpayerRequest",
                new XElement(EarNs + "taxNumber", taxNumber));

            var response = await SendSoapAsync("checkTaxpayer", bodyContent, ct).ConfigureAwait(false);

            var isRegistered = response.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "isRegistered")?.Value;

            return isRegistered?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GibPortal IsEInvoiceTaxpayer exception for {TaxNumber}", taxNumber);
            return false;
        }
    }

    public async Task<InvoiceResult> CancelInvoiceAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("GibPortal CancelInvoice for {GibInvoiceId}", gibInvoiceId);

        try
        {
            var bodyContent = new XElement(EarNs + "cancelInvoiceRequest",
                new XElement(EarNs + "gibInvoiceId", gibInvoiceId));

            var response = await SendSoapAsync("cancelInvoice", bodyContent, ct).ConfigureAwait(false);

            var success = response.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "success")?.Value;

            if (success?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
                return new InvoiceResult(true, gibInvoiceId, null, null);

            var errMsg = response.Descendants()
                             .FirstOrDefault(e => e.Name.LocalName == "errorMessage")?.Value
                         ?? "Cancel failed";
            return new InvoiceResult(false, gibInvoiceId, null, errMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GibPortal CancelInvoice exception for {GibInvoiceId}", gibInvoiceId);
            return new InvoiceResult(false, gibInvoiceId, null, ex.Message);
        }
    }

    // ── Private: SOAP infrastructure ─────────────────────────────────────

    private XDocument BuildEnvelope(XElement bodyContent)
    {
        return new XDocument(
            new XElement(SoapEnv + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", SoapEnv.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "ear", EarNs.NamespaceName),
                new XElement(SoapEnv + "Header",
                    new XElement(EarNs + "UserInfo",
                        new XElement(EarNs + "vkn", _vkn!),
                        new XElement(EarNs + "password", _password!))),
                new XElement(SoapEnv + "Body", bodyContent)));
    }

    private async Task<XElement> SendSoapAsync(string soapAction, XElement bodyContent, CancellationToken ct)
    {
        var envelope = BuildEnvelope(bodyContent);
        var xmlString = envelope.ToString(SaveOptions.DisableFormatting);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/earsiv-services/dispatch");
        request.Content = new StringContent(xmlString, Encoding.UTF8, "text/xml");
        request.Headers.Add("SOAPAction", soapAction);

        _logger.LogDebug("GibPortal SOAP request action={Action} to {BaseUrl}", soapAction, _baseUrl);

        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("GibPortal SOAP {Action} failed: {Status}", soapAction, response.StatusCode);
            throw new HttpRequestException($"SOAP request failed: {response.StatusCode}");
        }

        var doc = XDocument.Parse(content);

        var bodyEl = doc.Descendants(SoapEnv + "Body").FirstOrDefault()
                     ?? throw new InvalidOperationException("SOAP response has no Body element");

        // Check for SOAP Fault
        var fault = bodyEl.Descendants(SoapEnv + "Fault").FirstOrDefault()
                    ?? bodyEl.Descendants("Fault").FirstOrDefault();
        if (fault != null)
        {
            var faultString = fault.Element("faultstring")?.Value ?? "Unknown SOAP Fault";
            _logger.LogWarning("GibPortal SOAP Fault for action {Action}: {Fault}", soapAction, faultString);
            throw new InvalidOperationException($"SOAP Fault: {faultString}");
        }

        var resultEl = bodyEl.Elements().FirstOrDefault()
                       ?? throw new InvalidOperationException("SOAP response Body is empty");

        return resultEl;
    }

    // ── Private: XML line builders ────────────────────────────────────────

    private static XElement BuildInvoiceLines(IReadOnlyList<InvoiceLineDto> lines)
    {
        var linesEl = new XElement(EarNs + "lines");
        foreach (var line in lines)
        {
            linesEl.Add(new XElement(EarNs + "line",
                new XElement(EarNs + "productName", line.ProductName),
                new XElement(EarNs + "sku", line.SKU ?? string.Empty),
                new XElement(EarNs + "quantity", line.Quantity),
                new XElement(EarNs + "unitPrice", line.UnitPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement(EarNs + "taxRate", line.TaxRate.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement(EarNs + "taxAmount", line.TaxAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement(EarNs + "lineTotal", line.LineTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))));
        }
        return linesEl;
    }

    private static XElement BuildDispatchLines(IReadOnlyList<InvoiceLineDto> lines)
    {
        var linesEl = new XElement(EarNs + "lines");
        foreach (var line in lines)
        {
            linesEl.Add(new XElement(EarNs + "line",
                new XElement(EarNs + "productName", line.ProductName),
                new XElement(EarNs + "sku", line.SKU ?? string.Empty),
                new XElement(EarNs + "quantity", line.Quantity),
                new XElement(EarNs + "unitPrice", line.UnitPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))));
        }
        return linesEl;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "GibPortalProvider is not configured. Call Configure(vkn, password, baseUrl) first.");
    }
}
