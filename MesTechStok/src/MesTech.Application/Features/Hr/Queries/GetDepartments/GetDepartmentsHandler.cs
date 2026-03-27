using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Hr.Queries.GetDepartments;

public sealed class GetDepartmentsHandler
    : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    private readonly IDepartmentRepository _deptRepo;

    public GetDepartmentsHandler(IDepartmentRepository deptRepo)
        => _deptRepo = deptRepo ?? throw new ArgumentNullException(nameof(deptRepo));

    public async Task<IReadOnlyList<DepartmentDto>> Handle(
        GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var depts = await _deptRepo.GetByTenantAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return depts.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            Code = null,
            EmployeeCount = 0,
            IsActive = true
        }).ToList();
    }
}
