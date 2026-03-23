using FluentValidation;

namespace MesTech.Application.Features.Settings.Queries.GetGeneralSettings;

public class GetGeneralSettingsValidator : AbstractValidator<GetGeneralSettingsQuery>
{
    public GetGeneralSettingsValidator() { RuleFor(x => x.TenantId).NotEmpty(); }
}
