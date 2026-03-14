using Mapster;
using MediatR;
using MesTech.Application.DTOs.Tasks;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tasks.Queries.GetProjectTasks;

public class GetProjectTasksHandler : IRequestHandler<GetProjectTasksQuery, IReadOnlyList<WorkTaskDto>>
{
    private readonly IWorkTaskRepository _repository;

    public GetProjectTasksHandler(IWorkTaskRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<WorkTaskDto>> Handle(GetProjectTasksQuery req, CancellationToken ct)
    {
        var tasks = await _repository.GetByProjectAsync(req.ProjectId, req.Status, req.AssignedToUserId, ct);
        return tasks.Adapt<List<WorkTaskDto>>().AsReadOnly();
    }
}
