using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.DeactivateCampaign;

public class DeactivateCampaignCommandValidator : AbstractValidator<DeactivateCampaignCommand>
{
    public DeactivateCampaignCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli kampanya ID gerekli.");
    }
}
