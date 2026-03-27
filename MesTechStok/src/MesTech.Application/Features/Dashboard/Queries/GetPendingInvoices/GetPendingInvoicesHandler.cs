using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dashboard.Queries.GetPendingInvoices;

public sealed class GetPendingInvoicesHandler
    : IRequestHandler<GetPendingInvoicesQuery, IReadOnlyList<PendingInvoiceDto>>
{
    private readonly IInvoiceRepository _invoiceRepo;

    public GetPendingInvoicesHandler(IInvoiceRepository invoiceRepo)
        => _invoiceRepo = invoiceRepo ?? throw new ArgumentNullException(nameof(invoiceRepo));

    public async Task<IReadOnlyList<PendingInvoiceDto>> Handle(
        GetPendingInvoicesQuery request, CancellationToken cancellationToken)
    {
        // GetFailedAsync: Queued status faturaları (gönderim bekleyen)
        var invoices = await _invoiceRepo.GetFailedAsync(request.Count, cancellationToken)
            .ConfigureAwait(false);

        return invoices
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new PendingInvoiceDto
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                CustomerName = null,
                GrandTotal = i.GrandTotal,
                Status = i.Status.ToString(),
                CreatedAt = i.CreatedAt,
                DaysPending = (int)(DateTime.UtcNow - i.CreatedAt).TotalDays
            })
            .ToList();
    }
}
