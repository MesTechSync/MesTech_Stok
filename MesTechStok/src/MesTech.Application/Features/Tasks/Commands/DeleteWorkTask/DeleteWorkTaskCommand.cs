using MediatR;

namespace MesTech.Application.Features.Tasks.Commands.DeleteWorkTask;

public record DeleteWorkTaskCommand(Guid Id) : IRequest<DeleteWorkTaskResult>;
