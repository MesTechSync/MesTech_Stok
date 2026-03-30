using FluentValidation;

namespace MesTech.Application.Features.Dashboard.Queries.GetPendingInvoices;

public sealed class GetPendingInvoicesValidator : AbstractValidator<GetPendingInvoicesQuery>
{
    public GetPendingInvoicesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 100);
    }
}
