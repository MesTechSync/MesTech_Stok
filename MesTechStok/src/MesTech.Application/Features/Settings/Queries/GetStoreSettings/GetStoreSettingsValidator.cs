using FluentValidation;

namespace MesTech.Application.Features.Settings.Queries.GetStoreSettings;

public sealed class GetStoreSettingsValidator : AbstractValidator<GetStoreSettingsQuery>
{
    public GetStoreSettingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
