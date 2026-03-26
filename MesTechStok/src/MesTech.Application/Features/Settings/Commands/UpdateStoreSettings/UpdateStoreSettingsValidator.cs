using FluentValidation;

namespace MesTech.Application.Features.Settings.Commands.UpdateStoreSettings;

public sealed class UpdateStoreSettingsValidator : AbstractValidator<UpdateStoreSettingsCommand>
{
    public UpdateStoreSettingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => x.Email != null);
        RuleFor(x => x.Phone).MaximumLength(50).When(x => x.Phone != null);
        RuleFor(x => x.TaxNumber).MaximumLength(50).When(x => x.TaxNumber != null);
    }
}
