using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// Parasut adapter — IBulkInvoiceCapable only. Wraps existing ParasutInvoiceProvider.
/// </summary>
public class ParasutInvoiceAdapter : IInvoiceAdapter, IBulkInvoiceCapable
{
    private readonly ParasutInvoiceProvider _provider;
    private readonly ILogger<ParasutInvoiceAdapter> _logger;

    public ParasutInvoiceAdapter(ParasutInvoiceProvider provider, ILogger<ParasutInvoiceAdapter> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public string ProviderName => _provider.ProviderName;
    public InvoiceProviderType ProviderType => InvoiceProviderType.OnMuhasebe;
    public IInvoiceProvider Provider => _provider;

    public bool SupportsEFatura => true;
    public bool SupportsEArsiv => true;
    public bool SupportsEIrsaliye => true;
    public bool SupportsBulkInvoice => true;
    public bool SupportsIncomingInvoice => false;
    public bool SupportsAutoTypeDetection => false;
    public bool SupportsTemplateCustomization => false;
    public bool SupportsKontorBalance => false;

    public async Task<InvoiceResult> CreateInvoiceAsync(InvoiceCreateRequest request, CancellationToken ct = default)
    {
        var dto = MapToProviderDto(request);
        return request.Type switch
        {
            InvoiceType.EArsiv => await _provider.CreateEArsivAsync(dto, ct),
            InvoiceType.EIrsaliye => await _provider.CreateEIrsaliyeAsync(dto, ct),
            _ => await _provider.CreateEFaturaAsync(dto, ct)
        };
    }

    public Task<InvoiceResult> CancelInvoiceAsync(string invoiceId, string reason, CancellationToken ct = default)
        => _provider.CancelInvoiceAsync(invoiceId, ct);

    public async Task<InvoiceStatusInfo> GetInvoiceStatusAsync(string invoiceId, CancellationToken ct = default)
    {
        var result = await _provider.CheckStatusAsync(invoiceId, ct);
        return new InvoiceStatusInfo(result.GibInvoiceId, InvoiceStatus.Sent, result.Status, result.AcceptedAt, null);
    }

    public Task<byte[]> GetInvoicePdfAsync(string invoiceId, CancellationToken ct = default)
        => _provider.GetPdfAsync(invoiceId, ct);

    public Task<string> GetInvoiceXmlAsync(string invoiceId, CancellationToken ct = default)
    {
        _logger.LogWarning("GetInvoiceXmlAsync not supported by Parasut");
        return Task.FromResult("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Invoice />");
    }

    public Task<bool> IsEFaturaMukellefAsync(string vknOrTckn, CancellationToken ct = default)
        => _provider.IsEInvoiceTaxpayerAsync(vknOrTckn, ct);

    // ── IBulkInvoiceCapable ──

    public async Task<BulkInvoiceResult> CreateBulkInvoiceAsync(IEnumerable<InvoiceCreateRequest> requests, CancellationToken ct = default)
    {
        var results = new List<BulkInvoiceItemResult>();
        foreach (var request in requests)
        {
            try
            {
                var result = await CreateInvoiceAsync(request, ct);
                results.Add(new BulkInvoiceItemResult(request.OrderId, result.Success, result.GibInvoiceId, result.ErrorMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk invoice failed for OrderId={OrderId}", request.OrderId);
                results.Add(new BulkInvoiceItemResult(request.OrderId, false, null, ex.Message));
            }
        }

        return new BulkInvoiceResult(results.Count, results.Count(r => r.Success), results.Count(r => !r.Success), results);
    }

    private static InvoiceDto MapToProviderDto(InvoiceCreateRequest request)
    {
        return new InvoiceDto(
            InvoiceNumber: $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            CustomerName: request.Customer.Name,
            CustomerTaxNumber: request.Customer.TaxNumber,
            CustomerTaxOffice: request.Customer.TaxOffice,
            CustomerAddress: request.Customer.Address,
            SubTotal: request.TotalAmount,
            TaxTotal: request.TotalAmount * ((int)request.DefaultKdv / 100m),
            GrandTotal: request.TotalAmount * (1 + (int)request.DefaultKdv / 100m),
            Lines: request.Lines.Select(l => new InvoiceLineDto(
                l.ProductName, l.SKU ?? "", l.Quantity, l.UnitPrice,
                l.TaxRate, l.UnitPrice * l.Quantity * l.TaxRate,
                l.UnitPrice * l.Quantity * (1 + l.TaxRate)
            )).ToList()
        );
    }
}
