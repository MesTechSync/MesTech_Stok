using FluentValidation;

namespace MesTech.Application.Features.Dashboard.Queries.GetTopProducts;

public sealed class GetTopProductsValidator : AbstractValidator<GetTopProductsQuery>
{
    public GetTopProductsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}
