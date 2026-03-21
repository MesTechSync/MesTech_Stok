using FluentValidation;

namespace MesTech.Application.Commands.CreateCariHesap;

public class CreateCariHesapValidator : AbstractValidator<CreateCariHesapCommand>
{
    public CreateCariHesapValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TaxNumber).MaximumLength(500).When(x => x.TaxNumber != null);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Phone).MaximumLength(500).When(x => x.Phone != null);
        RuleFor(x => x.Email).MaximumLength(500).When(x => x.Email != null);
        RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address != null);
    }
}
