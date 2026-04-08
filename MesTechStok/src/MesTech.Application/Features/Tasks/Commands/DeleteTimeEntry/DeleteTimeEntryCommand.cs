using MediatR;

namespace MesTech.Application.Features.Tasks.Commands.DeleteTimeEntry;

public record DeleteTimeEntryCommand(Guid Id) : IRequest<DeleteTimeEntryResult>;
