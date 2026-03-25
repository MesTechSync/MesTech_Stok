using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// Trendyol e-Faturam adapter — IInvoiceAdapter composition wrapper.
/// Capabilities: e-Fatura, e-Arsiv, e-Irsaliye, Bulk, KontorBalance, Template.
/// NOT: IncomingInvoice not supported by Trendyol e-Faturam API.
/// </summary>
public sealed class TrendyolEFaturamAdapter : IInvoiceAdapter, IBulkInvoiceCapable, IKontorCapable, IInvoiceTemplateCapable
{
    private readonly TrendyolEFaturamProvider _provider;
    private readonly ILogger<TrendyolEFaturamAdapter> _logger;

    public TrendyolEFaturamAdapter(TrendyolEFaturamProvider provider, ILogger<TrendyolEFaturamAdapter> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ProviderName => _provider.ProviderName;
    public InvoiceProviderType ProviderType => InvoiceProviderType.GibEntegrator;
    public IInvoiceProvider Provider => _provider;

    public bool SupportsEFatura => true;
    public bool SupportsEArsiv => true;
    public bool SupportsEIrsaliye => true;
    public bool SupportsBulkInvoice => true;
    public bool SupportsIncomingInvoice => false;
    public bool SupportsAutoTypeDetection => true;
    public bool SupportsTemplateCustomization => true;
    public bool SupportsKontorBalance => true;

    // ── IInvoiceAdapter (6 universal) ──

    public async Task<InvoiceResult> CreateInvoiceAsync(InvoiceCreateRequest request, CancellationToken ct = default)
    {
        var dto = MapToProviderDto(request);
        var type = request.Type;

        if (type == InvoiceType.None)
        {
            if (!string.IsNullOrEmpty(request.Customer.TaxNumber))
            {
                var isMukellef = await _provider.IsEInvoiceTaxpayerAsync(request.Customer.TaxNumber, ct).ConfigureAwait(false);
                type = isMukellef ? InvoiceType.EFatura : InvoiceType.EArsiv;
                _logger.LogInformation("TrendyolEFaturam auto type detection: VKN={VKN} -> {Type}",
                    request.Customer.TaxNumber, type);
            }
            else
            {
                type = InvoiceType.EArsiv;
            }
        }

        return type switch
        {
            InvoiceType.EIrsaliye => await _provider.CreateEIrsaliyeAsync(dto, ct).ConfigureAwait(false),
            InvoiceType.EArsiv => await _provider.CreateEArsivAsync(dto, ct).ConfigureAwait(false),
            _ => await _provider.CreateEFaturaAsync(dto, ct).ConfigureAwait(false)
        };
    }

    public Task<InvoiceResult> CancelInvoiceAsync(string invoiceId, string reason, CancellationToken ct = default)
        => _provider.CancelInvoiceAsync(invoiceId, ct); // provider API has no reason param

    public async Task<InvoiceStatusInfo> GetInvoiceStatusAsync(string invoiceId, CancellationToken ct = default)
    {
        var result = await _provider.CheckStatusAsync(invoiceId, ct).ConfigureAwait(false);
        var status = result.Status switch
        {
            "Accepted" => InvoiceStatus.Accepted,
            "Rejected" => InvoiceStatus.Rejected,
            "Cancelled" => InvoiceStatus.Cancelled,
            "Error" => InvoiceStatus.Error,
            _ => InvoiceStatus.Sent
        };
        return new InvoiceStatusInfo(result.GibInvoiceId, status, result.Status, result.AcceptedAt, result.ErrorMessage);
    }

    public Task<byte[]> GetInvoicePdfAsync(string invoiceId, CancellationToken ct = default)
        => _provider.GetPdfAsync(invoiceId, ct);

    public Task<string> GetInvoiceXmlAsync(string invoiceId, CancellationToken ct = default)
    {
        _logger.LogWarning("GetInvoiceXmlAsync not supported by Trendyol e-Faturam API — returns empty XML");
        return Task.FromResult("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Invoice />");
    }

    public Task<bool> IsEFaturaMukellefAsync(string vknOrTckn, CancellationToken ct = default)
        => _provider.IsEInvoiceTaxpayerAsync(vknOrTckn, ct);

    // ── IBulkInvoiceCapable ──

    public Task<BulkInvoiceResult> CreateBulkInvoiceAsync(
        IEnumerable<InvoiceCreateRequest> requests, CancellationToken ct = default)
        => _provider.CreateBulkInvoiceAsync(requests, ct);

    // ── IKontorCapable ──

    public Task<KontorBalanceDto> GetKontorBalanceAsync(CancellationToken ct = default)
        => _provider.GetKontorBalanceAsync(ct);

    // ── IInvoiceTemplateCapable ──

    public Task<bool> SetInvoiceTemplateAsync(InvoiceTemplateDto template, CancellationToken ct = default)
        => _provider.SetInvoiceTemplateAsync(template, ct);

    // ── Private ──

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
