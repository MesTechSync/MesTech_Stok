using FluentValidation;

namespace MesTech.Application.Features.Settings.Queries.GetCredentialsSettings;

public class GetCredentialsSettingsValidator : AbstractValidator<GetCredentialsSettingsQuery>
{
    public GetCredentialsSettingsValidator() { RuleFor(x => x.TenantId).NotEmpty(); }
}
