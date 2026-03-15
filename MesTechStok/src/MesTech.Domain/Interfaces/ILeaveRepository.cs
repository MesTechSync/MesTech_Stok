using MesTech.Domain.Entities.Hr;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface ILeaveRepository
{
    Task<Leave?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Leave>> GetByTenantAsync(
        Guid tenantId, LeaveStatus? status = null, CancellationToken ct = default);
    Task<IReadOnlyList<Leave>> GetCurrentMonthAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Leave leave, CancellationToken ct = default);
}
