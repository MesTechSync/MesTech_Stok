using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Interfaces.Accounting;

public interface IReconciliationMatchRepository
{
    Task<ReconciliationMatch?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ReconciliationMatch>> GetByStatusAsync(Guid tenantId, ReconciliationStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<ReconciliationMatch>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(ReconciliationMatch match, CancellationToken ct = default);
    Task UpdateAsync(ReconciliationMatch match, CancellationToken ct = default);
}
