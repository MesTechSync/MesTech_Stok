using MesTech.Application.DTOs.Invoice;

namespace MesTech.Application.Interfaces;

public interface IBulkInvoiceCapable
{
    Task<BulkInvoiceResult> CreateBulkInvoiceAsync(
        IEnumerable<InvoiceDto> invoices, CancellationToken ct = default);
}
