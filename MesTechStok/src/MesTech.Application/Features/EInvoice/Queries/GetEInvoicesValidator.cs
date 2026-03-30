using FluentValidation;

namespace MesTech.Application.Features.EInvoice.Queries;

public sealed class GetEInvoicesValidator : AbstractValidator<GetEInvoicesQuery>
{
    public GetEInvoicesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.ProviderId).MaximumLength(200).When(x => x.ProviderId is not null);
    }
}
