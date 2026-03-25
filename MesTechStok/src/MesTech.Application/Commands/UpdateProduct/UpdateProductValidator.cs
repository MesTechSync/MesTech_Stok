using FluentValidation;

namespace MesTech.Application.Commands.UpdateProduct;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(500).When(x => x.Name != null);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
        RuleFor(x => x.Brand).MaximumLength(500).When(x => x.Brand != null);
    }
}
