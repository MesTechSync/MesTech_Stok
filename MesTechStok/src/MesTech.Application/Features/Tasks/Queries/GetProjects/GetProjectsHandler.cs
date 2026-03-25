using MediatR;
using MesTech.Application.DTOs.Tasks;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tasks.Queries.GetProjects;

public sealed class GetProjectsHandler : IRequestHandler<GetProjectsQuery, IReadOnlyList<ProjectDto>>
{
    private readonly IProjectRepository _repo;

    public GetProjectsHandler(IProjectRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var projects = await _repo.GetByTenantAsync(request.TenantId, cancellationToken);
        return projects.Select(p => new ProjectDto
        {
            Id = p.Id,
            Name = p.Name,
            Status = p.Status.ToString(),
            DueDate = p.DueDate,
            TaskCount = 0,
            CompletedTaskCount = 0
        }).ToList().AsReadOnly();
    }
}
