using FluentValidation;

namespace MesTech.Application.Features.Product.Commands.SaveProductVariants;

public sealed class SaveProductVariantsValidator : AbstractValidator<SaveProductVariantsCommand>
{
    public SaveProductVariantsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Variants).NotNull().NotEmpty()
            .WithMessage("En az bir varyant gereklidir.");

        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(x => x.SKU).NotEmpty().MaximumLength(100);
            v.RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
            v.RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
            v.RuleFor(x => x.Color).MaximumLength(100).When(x => x.Color is not null);
            v.RuleFor(x => x.Size).MaximumLength(50).When(x => x.Size is not null);
            v.RuleFor(x => x.Barcode).MaximumLength(50).When(x => x.Barcode is not null);
        });
    }
}
