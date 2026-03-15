using MesTech.Domain.Entities.Hr;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Employee>> GetByTenantAsync(
        Guid tenantId, EmployeeStatus? status = null, CancellationToken ct = default);
    Task AddAsync(Employee employee, CancellationToken ct = default);
}
