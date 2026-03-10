using MesTech.Application.DTOs.Invoice;
using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Fatura entegrator adapter — IInvoiceProvider'i composition ile sarar.
/// ISP: Sadece evrensel 6 metod. Ek capability'ler ayri interface'lerde.
/// </summary>
public interface IInvoiceAdapter
{
    // ── Kimlik ──
    string ProviderName { get; }
    InvoiceProviderType ProviderType { get; }
    IInvoiceProvider Provider { get; }

    // ── Capability Flags ──
    bool SupportsEFatura { get; }
    bool SupportsEArsiv { get; }
    bool SupportsEIrsaliye { get; }
    bool SupportsBulkInvoice { get; }
    bool SupportsIncomingInvoice { get; }
    bool SupportsAutoTypeDetection { get; }
    bool SupportsTemplateCustomization { get; }
    bool SupportsKontorBalance { get; }
    bool SupportsESMM => false;
    bool SupportsEIhracat => false;

    // ── Evrensel 6 Metod ──
    Task<InvoiceResult> CreateInvoiceAsync(InvoiceCreateRequest request, CancellationToken ct = default);
    Task<InvoiceResult> CancelInvoiceAsync(string invoiceId, string reason, CancellationToken ct = default);
    Task<InvoiceStatusInfo> GetInvoiceStatusAsync(string invoiceId, CancellationToken ct = default);
    Task<byte[]> GetInvoicePdfAsync(string invoiceId, CancellationToken ct = default);
    Task<string> GetInvoiceXmlAsync(string invoiceId, CancellationToken ct = default);
    Task<bool> IsEFaturaMukellefAsync(string vknOrTckn, CancellationToken ct = default);
}
