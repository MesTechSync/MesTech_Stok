using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.GetActiveCampaigns;

public sealed class GetActiveCampaignsValidator : AbstractValidator<GetActiveCampaignsQuery>
{
    public GetActiveCampaignsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
