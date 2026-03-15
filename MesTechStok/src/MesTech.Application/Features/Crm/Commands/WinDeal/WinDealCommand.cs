using MediatR;

namespace MesTech.Application.Features.Crm.Commands.WinDeal;

public record WinDealCommand(Guid DealId, Guid? OrderId = null) : IRequest<Unit>;
