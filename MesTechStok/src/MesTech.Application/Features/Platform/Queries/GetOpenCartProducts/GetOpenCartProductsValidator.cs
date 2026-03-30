using FluentValidation;

namespace MesTech.Application.Features.Platform.Queries.GetOpenCartProducts;

public sealed class GetOpenCartProductsValidator : AbstractValidator<GetOpenCartProductsQuery>
{
    public GetOpenCartProductsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.StoreId).NotEqual(Guid.Empty).WithMessage("StoreId bos olamaz.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
