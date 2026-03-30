using FluentValidation;

namespace MesTech.Application.Features.Invoice.Queries;

public sealed class GetInvoicesValidator : AbstractValidator<GetInvoicesQuery>
{
    public GetInvoicesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200)
            .When(x => x.Search is not null);
    }
}
