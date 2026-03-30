using FluentValidation;

namespace MesTech.Application.Features.Product.Commands.BulkCreateProducts;

public sealed class BulkCreateProductsValidator : AbstractValidator<BulkCreateProductsCommand>
{
    public BulkCreateProductsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Products)
            .NotEmpty().WithMessage("En az bir urun gereklidir.")
            .Must(p => p.Count <= 1000).WithMessage("Tek seferde en fazla 1000 urun olusturulabilir.");
    }
}
