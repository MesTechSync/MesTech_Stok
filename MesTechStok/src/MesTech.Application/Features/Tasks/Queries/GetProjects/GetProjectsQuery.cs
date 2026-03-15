using MediatR;
using MesTech.Application.DTOs.Tasks;

namespace MesTech.Application.Features.Tasks.Queries.GetProjects;

public record GetProjectsQuery(Guid TenantId) : IRequest<IReadOnlyList<ProjectDto>>;
