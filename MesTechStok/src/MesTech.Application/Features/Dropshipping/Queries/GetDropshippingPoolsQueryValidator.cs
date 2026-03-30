using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Queries;

public sealed class GetDropshippingPoolsQueryValidator : AbstractValidator<GetDropshippingPoolsQuery>
{
    public GetDropshippingPoolsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
