using FluentValidation;

namespace MesTech.Application.Commands.SaveCompanySettings;

public sealed class SaveCompanySettingsValidator : AbstractValidator<SaveCompanySettingsCommand>
{
    public SaveCompanySettingsValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TaxNumber).MaximumLength(500).When(x => x.TaxNumber != null);
        RuleFor(x => x.Phone).MaximumLength(500).When(x => x.Phone != null);
        RuleFor(x => x.Email).MaximumLength(500).When(x => x.Email != null);
        RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address != null);
    }
}
