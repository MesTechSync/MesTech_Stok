using FluentValidation;

namespace MesTech.Application.Features.Billing.Queries.GetBillingInvoices;

public sealed class GetBillingInvoicesValidator : AbstractValidator<GetBillingInvoicesQuery>
{
    public GetBillingInvoicesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
