using MediatR;
using MesTech.Application.DTOs.Tasks;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Tasks.Queries.GetProjectTasks;

public record GetProjectTasksQuery(Guid ProjectId, WorkTaskStatus? Status = null, Guid? AssignedToUserId = null)
    : IRequest<IReadOnlyList<WorkTaskDto>>;
