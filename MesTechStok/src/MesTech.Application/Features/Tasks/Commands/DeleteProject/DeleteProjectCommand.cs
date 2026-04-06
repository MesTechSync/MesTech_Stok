using MediatR;

namespace MesTech.Application.Features.Tasks.Commands.DeleteProject;

public record DeleteProjectCommand(Guid Id) : IRequest<DeleteProjectResult>;
