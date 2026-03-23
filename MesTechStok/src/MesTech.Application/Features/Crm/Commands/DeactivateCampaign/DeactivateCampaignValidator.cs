using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.DeactivateCampaign;

public class DeactivateCampaignValidator : AbstractValidator<DeactivateCampaignCommand>
{
    public DeactivateCampaignValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
    }
}
