using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// Sovos e-Fatura entegrasyonu — REST JSON API.
/// Sovos otomatik UBL olusturur, biz JSON gonderiyoruz.
/// Desteklenen islemler: e-Fatura, e-Arsiv, e-Irsaliye, durum sorgulama, PDF, iptal.
/// Dalga 9: IEInvoiceProvider eklendi — UBL-TR 1.2 XML, VKN mukellef sorgu, kredi bakiye.
/// </summary>
public class SovosInvoiceProvider : IInvoiceProvider, IBulkInvoiceCapable, IIncomingInvoiceCapable, IKontorCapable, IInvoiceTemplateCapable, IEInvoiceProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SovosInvoiceProvider> _logger;
    private readonly IUblTrXmlBuilder _ublBuilder;
    private string? _apiKey;
    private string? _baseUrl;
    private bool _isConfigured;

    public string ProviderName => "Sovos e-Fatura";
    public InvoiceProvider Provider => InvoiceProvider.Sovos;

    // IEInvoiceProvider
    public string ProviderCode => "Sovos";

    public SovosInvoiceProvider(
        HttpClient httpClient,
        ILogger<SovosInvoiceProvider> logger,
        IUblTrXmlBuilder ublBuilder)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ublBuilder = ublBuilder ?? throw new ArgumentNullException(nameof(ublBuilder));
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
        return await PostInvoiceAsync($"{_baseUrl}/api/invoices/outgoing", payload, ct).ConfigureAwait(false);
    }

    public async Task<InvoiceResult> CreateEArsivAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos CreateEArsiv for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildInvoicePayload(invoice, "EARSIV");
        return await PostInvoiceAsync($"{_baseUrl}/api/invoices/outgoing", payload, ct).ConfigureAwait(false);
    }

    public async Task<InvoiceResult> CreateEIrsaliyeAsync(InvoiceDto invoice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos CreateEIrsaliye for invoice {InvoiceNumber}", invoice.InvoiceNumber);

        var payload = BuildDispatchPayload(invoice);
        return await PostInvoiceAsync($"{_baseUrl}/api/dispatches/outgoing", payload, ct).ConfigureAwait(false);
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
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Sovos CheckStatus failed: {Status} — {Error}",
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
        return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
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

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
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
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
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

    // ── IBulkInvoiceCapable ────────────────────────────────────────────

    public async Task<BulkInvoiceResult> CreateBulkInvoiceAsync(
        IEnumerable<InvoiceCreateRequest> requests, CancellationToken ct = default)
    {
        EnsureConfigured();
        var requestList = requests.ToList();
        _logger.LogInformation("Sovos CreateBulkInvoice for {Count} invoices", requestList.Count);

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
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/api/invoices/outgoing/bulk", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Sovos CreateBulkInvoice failed: {Status} — {Error}",
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
                    var success = item.TryGetProperty("success", out var s) && s.GetBoolean();
                    var gibId = item.TryGetProperty("gibInvoiceId", out var g) ? g.GetString() : null;
                    var error = item.TryGetProperty("errorMessage", out var e) ? e.GetString() : null;
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
            _logger.LogError(ex, "Sovos CreateBulkInvoice exception");
            var failResults = requestList.Select(r =>
                new BulkInvoiceItemResult(r.OrderId, false, null, ex.Message)).ToList();
            return new BulkInvoiceResult(requestList.Count, 0, requestList.Count, failResults);
        }
    }

    // ── IIncomingInvoiceCapable ──────────────────────────────────────────

    public async Task<IReadOnlyList<IncomingInvoiceDto>> GetIncomingInvoicesAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos GetIncomingInvoices from {From} to {To}",
            from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        try
        {
            var fromStr = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var toStr = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/invoices/incoming?from={fromStr}&to={toStr}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sovos GetIncomingInvoices failed: {Status}", response.StatusCode);
                return Array.Empty<IncomingInvoiceDto>();
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var list = new List<IncomingInvoiceDto>();

            if (doc.RootElement.TryGetProperty("invoices", out var invoicesArray))
            {
                foreach (var item in invoicesArray.EnumerateArray())
                {
                    var gibId = item.GetProperty("gibInvoiceId").GetString()!;
                    var invoiceNumber = item.TryGetProperty("invoiceNumber", out var inv)
                        ? inv.GetString() ?? gibId : gibId;
                    var senderName = item.GetProperty("senderName").GetString()!;
                    var senderTax = item.GetProperty("senderTaxNumber").GetString()!;
                    var amount = item.GetProperty("amount").GetDecimal();
                    var invoiceDate = item.GetProperty("invoiceDate").GetDateTime();
                    var pdfUrl = item.TryGetProperty("pdfUrl", out var pdf)
                        && pdf.ValueKind != JsonValueKind.Null
                        ? pdf.GetString() : null;
                    var statusStr = item.GetProperty("status").GetString()!;
                    var status = Enum.TryParse<Domain.Enums.InvoiceStatus>(statusStr, true, out var parsed)
                        ? parsed : Domain.Enums.InvoiceStatus.Draft;
                    list.Add(new IncomingInvoiceDto(gibId, invoiceNumber, senderName, senderTax, amount, invoiceDate, pdfUrl, status));
                }
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sovos GetIncomingInvoices exception");
            return Array.Empty<IncomingInvoiceDto>();
        }
    }

    public async Task<bool> AcceptInvoiceAsync(string gibInvoiceId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos AcceptInvoice for {GibInvoiceId}", gibInvoiceId);

        try
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/api/invoices/incoming/{gibInvoiceId}/accept", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sovos AcceptInvoice failed: {Status}", response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sovos AcceptInvoice exception for {GibInvoiceId}", gibInvoiceId);
            return false;
        }
    }

    public async Task<bool> RejectInvoiceAsync(string gibInvoiceId, string reason, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos RejectInvoice for {GibInvoiceId}", gibInvoiceId);

        try
        {
            var payload = new { reason };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/api/invoices/incoming/{gibInvoiceId}/reject", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sovos RejectInvoice failed: {Status}", response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sovos RejectInvoice exception for {GibInvoiceId}", gibInvoiceId);
            return false;
        }
    }

    // ── IKontorCapable ──────────────────────────────────────────────────

    public async Task<KontorBalanceDto> GetKontorBalanceAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos GetKontorBalance");

        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/account/kontor", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sovos GetKontorBalance failed: {Status}", response.StatusCode);
                return new KontorBalanceDto(0, 0, null, ProviderName);
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var remaining = root.TryGetProperty("remaining", out var r) ? r.GetInt32() : 0;
            var total = root.TryGetProperty("total", out var t) ? t.GetInt32() : 0;
            DateTime? lastChecked = root.TryGetProperty("lastChecked", out var lc)
                && lc.ValueKind != JsonValueKind.Null
                ? lc.GetDateTime()
                : null;

            return new KontorBalanceDto(remaining, total, lastChecked, ProviderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sovos GetKontorBalance exception");
            return new KontorBalanceDto(0, 0, null, ProviderName);
        }
    }

    // ── IInvoiceTemplateCapable ─────────────────────────────────────────

    public async Task<bool> SetInvoiceTemplateAsync(InvoiceTemplateDto template, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos SetInvoiceTemplate");

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
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(
                $"{_baseUrl}/api/invoices/template", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sovos SetInvoiceTemplate failed: {Status}", response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sovos SetInvoiceTemplate exception");
            return false;
        }
    }

    // ── IEInvoiceProvider ────────────────────────────────────────────────

    /// <summary>
    /// UBL-TR 1.2 XML uret, Base64 encode et ve Sovos /einvoice/send endpoint'ine POST gonder.
    /// </summary>
    public async Task<EInvoiceSendResult> SendAsync(EInvoiceDocument document, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos EInvoice SendAsync for ETTN {EttnNo}", document.EttnNo);

        try
        {
            var xmlBytes = await _ublBuilder.BuildAsync(document, ct).ConfigureAwait(false);
            var xmlBase64 = Convert.ToBase64String(xmlBytes);

            var payload = new
            {
                ettnNo = document.EttnNo,
                gibUuid = document.GibUuid,
                scenario = document.Scenario.ToString(),
                xmlBase64
            };
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/einvoice/send", content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Sovos SendAsync failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return new EInvoiceSendResult(false, null, errorBody, 0);
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var providerRef = root.TryGetProperty("providerRef", out var pr) ? pr.GetString() : null;
            var creditUsed = root.TryGetProperty("creditUsed", out var cu) ? cu.GetInt32() : 1;

            return new EInvoiceSendResult(true, providerRef, null, creditUsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sovos SendAsync exception for ETTN {EttnNo}", document.EttnNo);
            return new EInvoiceSendResult(false, null, ex.Message, 0);
        }
    }

    public async Task<string?> GetPdfUrlAsync(string providerRef, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos GetPdfUrlAsync for providerRef {ProviderRef}", providerRef);

        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/einvoice/{providerRef}/pdf-url", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sovos GetPdfUrlAsync failed: {Status}", response.StatusCode);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.TryGetProperty("pdfUrl", out var url) ? url.GetString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sovos GetPdfUrlAsync exception for {ProviderRef}", providerRef);
            return null;
        }
    }

    public async Task<bool> CancelAsync(string providerRef, string reason, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos EInvoice CancelAsync for providerRef {ProviderRef}", providerRef);

        try
        {
            var payload = new { providerRef, reason };
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/einvoice/{providerRef}/cancel", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sovos CancelAsync failed: {Status}", response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sovos CancelAsync exception for {ProviderRef}", providerRef);
            return false;
        }
    }

    public async Task<VknMukellefResult> CheckVknMukellefAsync(string vkn, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos CheckVknMukellefAsync for VKN {Vkn}", vkn);

        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/einvoice/taxpayer/{vkn}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sovos CheckVknMukellefAsync failed: {Status} for VKN {Vkn}",
                    response.StatusCode, vkn);
                return new VknMukellefResult(vkn, false, false, null, null);
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var isEInvoice = root.TryGetProperty("isEInvoiceMukellef", out var ei) && ei.GetBoolean();
            var isEArchive = root.TryGetProperty("isEArchiveMukellef", out var ea) && ea.GetBoolean();
            var title = root.TryGetProperty("title", out var t) && t.ValueKind != JsonValueKind.Null
                ? t.GetString()
                : null;

            return new VknMukellefResult(vkn, isEInvoice, isEArchive, title, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sovos CheckVknMukellefAsync exception for VKN {Vkn}", vkn);
            return new VknMukellefResult(vkn, false, false, null, null);
        }
    }

    public async Task<int> GetCreditBalanceAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("Sovos GetCreditBalanceAsync");

        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/einvoice/credits", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sovos GetCreditBalanceAsync failed: {Status}", response.StatusCode);
                return 0;
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.TryGetProperty("balance", out var b) ? b.GetInt32() : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sovos GetCreditBalanceAsync exception");
            return 0;
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
            var response = await _httpClient.PostAsync(url, content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Sovos POST {Url} failed: {Status} — {Error}",
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sovos POST {Url} exception", url);
            return new InvoiceResult(false, null, null, ex.Message);
        }
    }
}
