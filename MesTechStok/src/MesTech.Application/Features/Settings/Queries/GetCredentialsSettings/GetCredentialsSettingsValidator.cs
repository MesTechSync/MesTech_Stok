using FluentValidation;

namespace MesTech.Application.Features.Settings.Queries.GetCredentialsSettings;

public sealed class GetCredentialsSettingsValidator : AbstractValidator<GetCredentialsSettingsQuery>
{
    public GetCredentialsSettingsValidator() { RuleFor(x => x.TenantId).NotEmpty(); }
}
