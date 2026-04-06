using FluentValidation;

namespace MesTech.Application.Commands.CreateBulkProducts;

public sealed class CreateBulkProductsValidator : AbstractValidator<CreateBulkProductsCommand>
{
    public CreateBulkProductsValidator()
    {
        RuleFor(x => x.Count)
            .GreaterThan(0).WithMessage("Ürün sayısı sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(500).WithMessage("Tek seferde en fazla 500 demo ürün oluşturulabilir.");
    }
}
