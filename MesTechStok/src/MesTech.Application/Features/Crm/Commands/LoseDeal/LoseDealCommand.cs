using MediatR;

namespace MesTech.Application.Features.Crm.Commands.LoseDeal;

public record LoseDealCommand(Guid DealId, string Reason) : IRequest<Unit>;
