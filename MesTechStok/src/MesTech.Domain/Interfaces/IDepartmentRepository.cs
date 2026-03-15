using MesTech.Domain.Entities.Hr;

namespace MesTech.Domain.Interfaces;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Department>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Department department, CancellationToken ct = default);
}
