using FluentValidation;

namespace MesTech.Application.Features.Settings.Queries.GetImportSettings;

public sealed class GetImportSettingsValidator : AbstractValidator<GetImportSettingsQuery>
{
    public GetImportSettingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
