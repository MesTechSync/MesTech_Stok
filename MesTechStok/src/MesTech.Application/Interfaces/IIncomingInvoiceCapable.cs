using MesTech.Application.DTOs.Invoice;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Gelen fatura yonetimi capability — sadece GIB entegratorleri (Sovos).
/// </summary>
public interface IIncomingInvoiceCapable
{
    Task<IReadOnlyList<IncomingInvoiceDto>> GetIncomingInvoicesAsync(
        DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<bool> AcceptInvoiceAsync(string invoiceId, CancellationToken ct = default);
    Task<bool> RejectInvoiceAsync(string invoiceId, string reason, CancellationToken ct = default);
}
