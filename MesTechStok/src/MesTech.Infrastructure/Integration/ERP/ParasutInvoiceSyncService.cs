using System.Net.Http.Json;
using System.Text.Json;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.ERP.Parasut;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesTech.Infrastructure.Integration.ERP;

/// <summary>
/// Paraşüt fatura sync servisi.
/// MesTech faturalarını Paraşüt'e satış faturası olarak gönderir,
/// ardından e-Fatura veya e-Arşiv oluşturur.
/// </summary>
public interface IParasutInvoiceSyncService
{
    Task<string?> CreateSalesInvoiceAsync(ParasutInvoiceRequest request, CancellationToken ct = default);
    Task<string?> CreateEInvoiceAsync(string salesInvoiceId, CancellationToken ct = default);
    Task<string?> CreateEArchiveAsync(string salesInvoiceId, CancellationToken ct = default);
    Task<byte[]?> GetInvoicePdfAsync(string eInvoiceId, CancellationToken ct = default);
}

public sealed class ParasutInvoiceSyncService : IParasutInvoiceSyncService
{
    private readonly HttpClient _httpClient;
    private readonly ParasutOptions _options;
    private readonly ILogger<ParasutInvoiceSyncService> _logger;

    public ParasutInvoiceSyncService(
        IHttpClientFactory httpClientFactory,
        IOptions<ParasutOptions> options,
        ILogger<ParasutInvoiceSyncService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Parasut");
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> CreateSalesInvoiceAsync(ParasutInvoiceRequest request, CancellationToken ct = default)
    {
        var body = new
        {
            data = new
            {
                type = "sales_invoices",
                attributes = new
                {
                    item_type = "invoice",
                    description = request.Description,
                    issue_date = request.IssueDate.ToString("yyyy-MM-dd"),
                    due_date = request.DueDate?.ToString("yyyy-MM-dd"),
                    invoice_series = "MES",
                    invoice_id = request.InvoiceNumber,
                    currency = request.Currency ?? "TRY"
                }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"/v4/{_options.CompanyId}/sales_invoices", body, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
        var id = result.GetProperty("data").GetProperty("id").GetString();
        _logger.LogInformation("Paraşüt sales invoice created: {Id}", id);
        return id;
    }

    public async Task<string?> CreateEInvoiceAsync(string salesInvoiceId, CancellationToken ct = default)
    {
        var body = new
        {
            data = new
            {
                type = "e_invoices",
                relationships = new
                {
                    invoice = new { data = new { id = salesInvoiceId, type = "sales_invoices" } }
                }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"/v4/{_options.CompanyId}/e_invoices", body, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
        return result.GetProperty("data").GetProperty("id").GetString();
    }

    public async Task<string?> CreateEArchiveAsync(string salesInvoiceId, CancellationToken ct = default)
    {
        var body = new
        {
            data = new
            {
                type = "e_archives",
                relationships = new
                {
                    sales_invoice = new { data = new { id = salesInvoiceId, type = "sales_invoices" } }
                }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"/v4/{_options.CompanyId}/e_archives", body, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
        return result.GetProperty("data").GetProperty("id").GetString();
    }

    public async Task<byte[]?> GetInvoicePdfAsync(string eInvoiceId, CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(
            $"/v4/{_options.CompanyId}/e_invoices/{eInvoiceId}/pdf", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
    }
}

public record ParasutInvoiceRequest(
    string InvoiceNumber,
    string Description,
    DateTime IssueDate,
    DateTime? DueDate,
    string? Currency,
    bool IsEInvoiceTaxpayer,
    List<ParasutInvoiceLineRequest> Lines);

public record ParasutInvoiceLineRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate);
