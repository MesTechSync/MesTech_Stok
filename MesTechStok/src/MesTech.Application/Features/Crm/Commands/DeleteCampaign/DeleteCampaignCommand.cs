using MediatR;

namespace MesTech.Application.Features.Crm.Commands.DeleteCampaign;

public record DeleteCampaignCommand(Guid Id) : IRequest<DeleteCampaignResult>;
