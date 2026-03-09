using MesTech.Application.DTOs.Invoice;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Toplu fatura olusturma capability — Sovos, Parasut.
/// </summary>
public interface IBulkInvoiceCapable
{
    Task<BulkInvoiceResult> CreateBulkInvoiceAsync(
        IEnumerable<InvoiceCreateRequest> requests,
        CancellationToken ct = default);
}
