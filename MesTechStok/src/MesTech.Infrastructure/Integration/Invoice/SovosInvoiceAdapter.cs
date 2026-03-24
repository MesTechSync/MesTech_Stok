using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Security;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// Sovos adapter — full 4 capability. Wraps existing SovosInvoiceProvider.
/// </summary>
public class SovosInvoiceAdapter : IInvoiceAdapter, IBulkInvoiceCapable, IIncomingInvoiceCapable, IKontorCapable, IInvoiceTemplateCapable
{
    private readonly SovosInvoiceProvider _provider;
    private readonly IGibMukellefService _gibService;
    private readonly ILogger<SovosInvoiceAdapter> _logger;

    public SovosInvoiceAdapter(SovosInvoiceProvider provider, IGibMukellefService gibService, ILogger<SovosInvoiceAdapter> logger)
    {
        _provider = provider;
        _gibService = gibService;
        _logger = logger;
    }

    public string ProviderName => _provider.ProviderName;
    public InvoiceProviderType ProviderType => InvoiceProviderType.GibEntegrator;
    public IInvoiceProvider Provider => _provider;

    public bool SupportsEFatura => true;
    public bool SupportsEArsiv => true;
    public bool SupportsEIrsaliye => true;
    public bool SupportsBulkInvoice => true;
    public bool SupportsIncomingInvoice => true;
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
                var isMukellef = await _gibService.IsEFaturaMukellefAsync(request.Customer.TaxNumber, ct).ConfigureAwait(false);
                type = isMukellef ? InvoiceType.EFatura : InvoiceType.EArsiv;
                _logger.LogInformation("Auto type detection via GibService: VKN={VKN} -> {Type}", PiiLogMaskHelper.MaskTaxNumber(request.Customer.TaxNumber), type);
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
        => _provider.CancelInvoiceAsync(invoiceId, ct);

    public async Task<InvoiceStatusInfo> GetInvoiceStatusAsync(string invoiceId, CancellationToken ct = default)
    {
        var result = await _provider.CheckStatusAsync(invoiceId, ct).ConfigureAwait(false);
        return new InvoiceStatusInfo(result.GibInvoiceId, InvoiceStatus.Sent, result.Status, result.AcceptedAt, null);
    }

    public Task<byte[]> GetInvoicePdfAsync(string invoiceId, CancellationToken ct = default)
        => _provider.GetPdfAsync(invoiceId, ct);

    public async Task<string> GetInvoiceXmlAsync(string invoiceId, CancellationToken ct = default)
    {
        // Sovos PDF endpoint'i kullanarak XML proxy — tam UBL-TR XML çekimi
        // Sovos REST API'de ayrı XML endpoint yoksa PDF base64 döner
        var pdf = await _provider.GetPdfAsync(invoiceId, ct).ConfigureAwait(false);
        return Convert.ToBase64String(pdf);
    }

    public Task<bool> IsEFaturaMukellefAsync(string vknOrTckn, CancellationToken ct = default)
        => _gibService.IsEFaturaMukellefAsync(vknOrTckn, ct);

    // ── IBulkInvoiceCapable ──

    public async Task<BulkInvoiceResult> CreateBulkInvoiceAsync(IEnumerable<InvoiceCreateRequest> requests, CancellationToken ct = default)
    {
        var results = new List<BulkInvoiceItemResult>();
        foreach (var request in requests)
        {
            try
            {
                var result = await CreateInvoiceAsync(request, ct).ConfigureAwait(false);
                results.Add(new BulkInvoiceItemResult(request.OrderId, result.Success, result.GibInvoiceId, result.ErrorMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk invoice failed for OrderId={OrderId}", request.OrderId);
                results.Add(new BulkInvoiceItemResult(request.OrderId, false, null, ex.Message));
            }
        }

        return new BulkInvoiceResult(
            results.Count,
            results.Count(r => r.Success),
            results.Count(r => !r.Success),
            results);
    }

    // ── IIncomingInvoiceCapable ──

    public Task<IReadOnlyList<IncomingInvoiceDto>> GetIncomingInvoicesAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        => _provider.GetIncomingInvoicesAsync(startDate, endDate, ct);

    public Task<bool> AcceptInvoiceAsync(string invoiceId, CancellationToken ct = default)
        => _provider.AcceptInvoiceAsync(invoiceId, ct);

    public Task<bool> RejectInvoiceAsync(string invoiceId, string reason, CancellationToken ct = default)
        => _provider.RejectInvoiceAsync(invoiceId, reason, ct);

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
