using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.GetCrmSettings;

public sealed class GetCrmSettingsValidator : AbstractValidator<GetCrmSettingsQuery>
{
    public GetCrmSettingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
