using FluentValidation;

namespace MesTech.Application.Commands.BulkUpdatePrice;

public sealed class BulkUpdatePriceValidator : AbstractValidator<BulkUpdatePriceCommand>
{
    public BulkUpdatePriceValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("En az bir fiyat güncelleme öğesi gereklidir.")
            .Must(items => items.Count <= 1000).WithMessage("Tek seferde en fazla 1000 ürün güncellenebilir.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Sku)
                .NotEmpty().WithMessage("SKU boş olamaz.")
                .MaximumLength(100).WithMessage("SKU en fazla 100 karakter.");
            item.RuleFor(i => i.NewPrice)
                .GreaterThan(0).WithMessage("Fiyat sıfırdan büyük olmalı.");
        });
    }
}
