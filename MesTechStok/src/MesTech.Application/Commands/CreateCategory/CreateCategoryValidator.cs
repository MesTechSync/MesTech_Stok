using FluentValidation;

namespace MesTech.Application.Commands.CreateCategory;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kategori adı zorunludur.")
            .MaximumLength(200).WithMessage("Kategori adı en fazla 200 karakter.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Kategori kodu zorunludur.")
            .MaximumLength(50).WithMessage("Kategori kodu en fazla 50 karakter.")
            .Matches(@"^[A-Za-z0-9_-]+$").WithMessage("Kategori kodu yalnızca harf, rakam, tire ve alt çizgi içerebilir.");

    }
}
