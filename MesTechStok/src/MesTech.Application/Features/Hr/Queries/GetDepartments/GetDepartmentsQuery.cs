using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Hr.Queries.GetDepartments;

public record GetDepartmentsQuery(Guid TenantId) : IRequest<IReadOnlyList<DepartmentDto>>, ICacheableQuery
{
    public string CacheKey => $"Departments_{TenantId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public sealed class DepartmentDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Code { get; init; }
    public int EmployeeCount { get; init; }
    public bool IsActive { get; init; }
}
