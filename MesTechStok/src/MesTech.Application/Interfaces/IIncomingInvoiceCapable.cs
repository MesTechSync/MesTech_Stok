using MesTech.Application.DTOs.Invoice;

namespace MesTech.Application.Interfaces;

public interface IIncomingInvoiceCapable
{
    Task<IReadOnlyList<IncomingInvoiceDto>> GetIncomingInvoicesAsync(
        DateTime from, DateTime to, CancellationToken ct = default);
    Task<bool> AcceptInvoiceAsync(string gibInvoiceId, CancellationToken ct = default);
    Task<bool> RejectInvoiceAsync(string gibInvoiceId, string reason, CancellationToken ct = default);
}
