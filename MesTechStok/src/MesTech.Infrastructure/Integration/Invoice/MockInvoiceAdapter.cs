using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// Mock adapter — no capability interfaces. Test/development only.
/// Wraps existing MockInvoiceProvider via composition.
/// </summary>
public class MockInvoiceAdapter : IInvoiceAdapter
{
    private readonly MockInvoiceProvider _provider;

    public MockInvoiceAdapter(MockInvoiceProvider provider)
    {
        _provider = provider;
    }

    public string ProviderName => _provider.ProviderName;
    public InvoiceProviderType ProviderType => InvoiceProviderType.GibEntegrator;
    public IInvoiceProvider Provider => _provider;

    public bool SupportsEFatura => true;
    public bool SupportsEArsiv => true;
    public bool SupportsEIrsaliye => true;
    public bool SupportsBulkInvoice => false;
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
        return new InvoiceStatusInfo(result.GibInvoiceId, InvoiceStatus.Accepted, result.Status, result.AcceptedAt, null);
    }

    public Task<byte[]> GetInvoicePdfAsync(string invoiceId, CancellationToken ct = default)
        => _provider.GetPdfAsync(invoiceId, ct);

    public Task<string> GetInvoiceXmlAsync(string invoiceId, CancellationToken ct = default)
        => Task.FromResult("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Invoice><Note>Mock XML</Note></Invoice>");

    public Task<bool> IsEFaturaMukellefAsync(string vknOrTckn, CancellationToken ct = default)
        => _provider.IsEInvoiceTaxpayerAsync(vknOrTckn, ct);

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
