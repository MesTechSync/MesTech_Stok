using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Billing.Queries.GetBillingInvoices;

public sealed class GetBillingInvoicesHandler : IRequestHandler<GetBillingInvoicesQuery, IReadOnlyList<BillingInvoiceDto>>
{
    private readonly IBillingInvoiceRepository _repository;

    public GetBillingInvoicesHandler(IBillingInvoiceRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<BillingInvoiceDto>> Handle(GetBillingInvoicesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var invoices = await _repository.GetByTenantIdAsync(request.TenantId, cancellationToken);
        return invoices.Select(i => new BillingInvoiceDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            Amount = i.Amount,
            TaxAmount = i.TaxAmount,
            TotalAmount = i.TotalAmount,
            CurrencyCode = i.CurrencyCode,
            Status = i.Status,
            IssueDate = i.IssueDate,
            DueDate = i.DueDate,
            PaidAt = i.PaidAt
        }).ToList().AsReadOnly();
    }
}
