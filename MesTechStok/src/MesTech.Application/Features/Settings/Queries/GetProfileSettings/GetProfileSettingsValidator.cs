using FluentValidation;

namespace MesTech.Application.Features.Settings.Queries.GetProfileSettings;

public class GetProfileSettingsValidator : AbstractValidator<GetProfileSettingsQuery>
{
    public GetProfileSettingsValidator() { RuleFor(x => x.TenantId).NotEmpty(); }
}
