using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface ISyncLogRepository
{
    Task<SyncLog?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<SyncLog>> GetRecentAsync(Guid tenantId, int count, string? platformFilter = null, CancellationToken ct = default);
    Task AddAsync(SyncLog syncLog, CancellationToken ct = default);
}
