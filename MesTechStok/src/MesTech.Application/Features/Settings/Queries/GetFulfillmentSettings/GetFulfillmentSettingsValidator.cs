using FluentValidation;

namespace MesTech.Application.Features.Settings.Queries.GetFulfillmentSettings;

public sealed class GetFulfillmentSettingsValidator : AbstractValidator<GetFulfillmentSettingsQuery>
{
    public GetFulfillmentSettingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
