using FluentValidation;

namespace MesTech.Application.Commands.UpdateProduct;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500).When(x => x.Name != null);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description != null);
        RuleFor(x => x.Brand).MaximumLength(200).When(x => x.Brand != null);

        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0)
            .WithMessage("Purchase price cannot be negative.")
            .When(x => x.PurchasePrice.HasValue);
        RuleFor(x => x.SalePrice).GreaterThan(0)
            .WithMessage("Sale price must be greater than zero.")
            .When(x => x.SalePrice.HasValue);
        RuleFor(x => x.TaxRate).InclusiveBetween(0, 1)
            .WithMessage("Tax rate must be between 0 and 1.")
            .When(x => x.TaxRate.HasValue);
        RuleFor(x => x.MinimumStock).GreaterThanOrEqualTo(0)
            .When(x => x.MinimumStock.HasValue);
        RuleFor(x => x.MaximumStock).GreaterThan(0)
            .When(x => x.MaximumStock.HasValue);
    }
}
