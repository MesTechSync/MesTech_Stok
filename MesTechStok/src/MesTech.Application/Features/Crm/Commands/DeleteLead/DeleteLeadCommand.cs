using MediatR;

namespace MesTech.Application.Features.Crm.Commands.DeleteLead;

public record DeleteLeadCommand(Guid Id) : IRequest<DeleteLeadResult>;
