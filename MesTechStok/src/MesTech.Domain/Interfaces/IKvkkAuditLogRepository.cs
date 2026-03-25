using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IKvkkAuditLogRepository
{
    Task AddAsync(KvkkAuditLog log, CancellationToken ct = default);
    Task<(IReadOnlyList<KvkkAuditLog> Items, int TotalCount)> GetByTenantPagedAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct = default);
}
