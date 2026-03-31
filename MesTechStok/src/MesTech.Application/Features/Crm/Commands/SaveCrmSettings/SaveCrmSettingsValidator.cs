using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.SaveCrmSettings;

public sealed class SaveCrmSettingsValidator : AbstractValidator<SaveCrmSettingsCommand>
{
    public SaveCrmSettingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.LeadScoreThreshold).InclusiveBetween(0, 100);
    }
}
