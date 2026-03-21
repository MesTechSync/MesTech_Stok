using FluentValidation;

namespace MesTech.Application.Commands.UpdateProductPrice;

public class UpdateProductPriceValidator : AbstractValidator<UpdateProductPriceCommand>
{
    public UpdateProductPriceValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SKU).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
