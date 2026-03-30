using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Queries;

public sealed class GetPoolProductsQueryValidator : AbstractValidator<GetPoolProductsQuery>
{
    public GetPoolProductsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(500)
            .When(x => x.Search is not null);
    }
}
