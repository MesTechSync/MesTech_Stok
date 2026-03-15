using Mapster;
using MediatR;
using MesTech.Application.DTOs.Tasks;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tasks.Queries.GetProjectTasks;

public class GetProjectTasksHandler : IRequestHandler<GetProjectTasksQuery, IReadOnlyList<WorkTaskDto>>
{
    private readonly IWorkTaskRepository _repository;

    public GetProjectTasksHandler(IWorkTaskRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<WorkTaskDto>> Handle(GetProjectTasksQuery request, CancellationToken cancellationToken)
    {
        var tasks = await _repository.GetByProjectAsync(request.ProjectId, request.Status, request.AssignedToUserId, cancellationToken);
        return tasks.Adapt<List<WorkTaskDto>>().AsReadOnly();
    }
}
