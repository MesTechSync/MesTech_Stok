using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Hr.Queries.GetEmployees;

public record GetEmployeesQuery(Guid TenantId, EmployeeStatus? Status = null)
    : IRequest<IReadOnlyList<EmployeeDto>>;
