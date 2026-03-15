using MediatR;

namespace MesTech.Application.Features.Tasks.Commands.CompleteTask;

public record CompleteTaskCommand(Guid TaskId, Guid UserId) : IRequest<Unit>;
