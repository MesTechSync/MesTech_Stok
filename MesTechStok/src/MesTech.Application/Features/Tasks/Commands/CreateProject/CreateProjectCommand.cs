using MediatR;

namespace MesTech.Application.Features.Tasks.Commands.CreateProject;

public record CreateProjectCommand(
    Guid TenantId, string Name, Guid? OwnerUserId = null,
    string? Description = null, DateTime? StartDate = null,
    DateTime? DueDate = null, string? Color = null
) : IRequest<Guid>;
