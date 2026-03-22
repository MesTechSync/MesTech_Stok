using MediatR;

namespace MesTech.Application.Features.Crm.Commands.DeactivateCampaign;

public record DeactivateCampaignCommand(Guid CampaignId) : IRequest<Unit>;
