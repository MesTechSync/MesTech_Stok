using FluentValidation;

namespace MesTech.Application.Features.Product.Commands.BulkUpdateProducts;

public class BulkUpdateProductsValidator : AbstractValidator<BulkUpdateProductsCommand>
{
    public BulkUpdateProductsValidator()
    {
        RuleFor(x => x.ProductIds)
            .NotEmpty().WithMessage("En az bir ürün seçilmelidir.")
            .Must(ids => ids.Count <= 500).WithMessage("Tek seferde en fazla 500 ürün güncellenebilir.");

        RuleFor(x => x.Action)
            .IsInEnum().WithMessage("Geçersiz toplu güncelleme işlemi.");
    }
}
