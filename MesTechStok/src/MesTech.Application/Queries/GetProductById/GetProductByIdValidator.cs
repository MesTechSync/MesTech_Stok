using FluentValidation;

namespace MesTech.Application.Queries.GetProductById;

public sealed class GetProductByIdValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdValidator()
    {
        RuleFor(x => x.ProductId).NotEqual(Guid.Empty).WithMessage("Geçerli ürün ID gerekli.");
    }
}
