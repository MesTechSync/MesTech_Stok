using MesTech.Domain.Entities.Reporting;

namespace MesTech.Application.Interfaces.Reporting;

public interface IKpiSnapshotRepository
{
    Task<KpiSnapshot?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<KpiSnapshot?> GetLatestByTypeAsync(Guid tenantId, KpiType type, CancellationToken ct = default);
    Task<IReadOnlyList<KpiSnapshot>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(KpiSnapshot snapshot, CancellationToken ct = default);
}
