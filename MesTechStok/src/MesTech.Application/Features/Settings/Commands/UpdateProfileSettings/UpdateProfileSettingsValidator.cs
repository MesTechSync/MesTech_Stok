using FluentValidation;

namespace MesTech.Application.Features.Settings.Commands.UpdateProfileSettings;

public class UpdateProfileSettingsValidator : AbstractValidator<UpdateProfileSettingsCommand>
{
    public UpdateProfileSettingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxNumber).MaximumLength(20);
    }
}
