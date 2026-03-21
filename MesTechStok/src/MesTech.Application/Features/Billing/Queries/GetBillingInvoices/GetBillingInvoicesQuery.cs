using MediatR;
using MesTech.Domain.Entities.Billing;

namespace MesTech.Application.Features.Billing.Queries.GetBillingInvoices;

public record GetBillingInvoicesQuery(Guid TenantId) : IRequest<IReadOnlyList<BillingInvoiceDto>>;

public record BillingInvoiceDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string CurrencyCode { get; init; } = "TRY";
    public BillingInvoiceStatus Status { get; init; }
    public DateTime IssueDate { get; init; }
    public DateTime DueDate { get; init; }
    public DateTime? PaidAt { get; init; }
}
