using FluentValidation;

namespace MesTech.Application.Features.Product.Queries.GetProductVariants;

public sealed class GetProductVariantsValidator : AbstractValidator<GetProductVariantsQuery>
{
    public GetProductVariantsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ProductId).NotEqual(Guid.Empty).WithMessage("Geçerli ürün ID gerekli.");
    }
}
