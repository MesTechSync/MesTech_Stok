using FluentValidation;

namespace MesTech.Application.Features.Settings.Commands.SaveFulfillmentSettings;

public sealed class SaveFulfillmentSettingsValidator : AbstractValidator<SaveFulfillmentSettingsCommand>
{
    public SaveFulfillmentSettingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
