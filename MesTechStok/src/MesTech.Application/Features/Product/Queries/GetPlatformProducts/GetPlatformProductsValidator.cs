using FluentValidation;

namespace MesTech.Application.Features.Product.Queries.GetPlatformProducts;

public sealed class GetPlatformProductsValidator : AbstractValidator<GetPlatformProductsQuery>
{
    public GetPlatformProductsValidator()
    {
        RuleFor(x => x.PlatformCode).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
