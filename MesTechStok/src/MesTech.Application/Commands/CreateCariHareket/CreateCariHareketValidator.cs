using FluentValidation;

namespace MesTech.Application.Commands.CreateCariHareket;

public sealed class CreateCariHareketValidator : AbstractValidator<CreateCariHareketCommand>
{
    public CreateCariHareketValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CariHesapId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Tutar sıfırdan büyük olmalıdır.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
    }
}
