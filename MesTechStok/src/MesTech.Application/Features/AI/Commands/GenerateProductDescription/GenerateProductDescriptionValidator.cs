using FluentValidation;

namespace MesTech.Application.Features.AI.Commands.GenerateProductDescription;

public sealed class GenerateProductDescriptionValidator : AbstractValidator<GenerateProductDescriptionCommand>
{
    public GenerateProductDescriptionValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli ürün ID gerekli.");

        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId zorunlu.");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Ürün adı zorunludur.")
            .MaximumLength(500).WithMessage("Ürün adı en fazla 500 karakter.");

        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Dil kodu zorunludur.")
            .MaximumLength(5).WithMessage("Dil kodu en fazla 5 karakter (ör: tr, en).");
    }
}
