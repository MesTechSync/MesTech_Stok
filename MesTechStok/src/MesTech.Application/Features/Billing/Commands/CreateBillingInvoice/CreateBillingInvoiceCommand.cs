using MediatR;

namespace MesTech.Application.Features.Billing.Commands.CreateBillingInvoice;

public record CreateBillingInvoiceCommand(
    Guid TenantId, Guid SubscriptionId,
    decimal Amount, string CurrencyCode = "TRY",
    decimal TaxRate = 0.20m, int DueDays = 7
) : IRequest<Guid>;
