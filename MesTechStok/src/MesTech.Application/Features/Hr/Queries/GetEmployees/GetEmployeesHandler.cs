using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Hr.Queries.GetEmployees;

public class GetEmployeesHandler : IRequestHandler<GetEmployeesQuery, IReadOnlyList<EmployeeDto>>
{
    private readonly IEmployeeRepository _employees;

    public GetEmployeesHandler(IEmployeeRepository employees) => _employees = employees;

    public async Task<IReadOnlyList<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var employees = await _employees.GetByTenantAsync(request.TenantId, request.Status, cancellationToken);
        return employees.Select(e => new EmployeeDto
        {
            Id = e.Id,
            EmployeeCode = e.EmployeeCode,
            JobTitle = e.JobTitle ?? string.Empty,
            WorkEmail = e.WorkEmail ?? string.Empty,
            Status = e.Status.ToString(),
            HireDate = e.HireDate,
            DepartmentId = e.DepartmentId
        }).ToList();
    }
}
