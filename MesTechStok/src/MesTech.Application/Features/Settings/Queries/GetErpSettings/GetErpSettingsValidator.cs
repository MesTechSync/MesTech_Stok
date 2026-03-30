using FluentValidation;

namespace MesTech.Application.Features.Settings.Queries.GetErpSettings;

public sealed class GetErpSettingsValidator : AbstractValidator<GetErpSettingsQuery>
{
    public GetErpSettingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
