using MediatR;

namespace MesTech.Application.Features.Crm.Commands.DeleteDeal;

public record DeleteDealCommand(Guid Id) : IRequest<DeleteDealResult>;
