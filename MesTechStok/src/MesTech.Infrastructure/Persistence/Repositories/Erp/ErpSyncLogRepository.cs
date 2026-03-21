using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories.Erp;

public class ErpSyncLogRepository : IErpSyncLogRepository
{
    private readonly AppDbContext _context;

    public ErpSyncLogRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(ErpSyncLog log, CancellationToken ct = default)
        => await _context.ErpSyncLogs.AddAsync(log, ct);

    public Task UpdateAsync(ErpSyncLog log, CancellationToken ct = default)
    {
        _context.ErpSyncLogs.Update(log);
        return Task.CompletedTask;
    }

    public async Task<ErpSyncLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ErpSyncLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<ErpSyncLog?> GetLatestByEntityAsync(
        Guid tenantId,
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
        => await _context.ErpSyncLogs
            .Where(e => e.TenantId == tenantId
                && e.EntityType == entityType
                && e.EntityId == entityId)
            .OrderByDescending(e => e.CreatedAt)
            .AsNoTracking()
            .AsNoTracking().FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ErpSyncLog>> GetPendingRetriesAsync(
        Guid tenantId,
        DateTime asOf,
        CancellationToken ct = default)
        => await _context.ErpSyncLogs
            .Where(e => e.TenantId == tenantId
                && e.NextRetryAt != null
                && e.NextRetryAt <= asOf)
            .AsNoTracking()
            .AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<ErpSyncLog>> GetFailedByProviderAsync(
        Guid tenantId,
        ErpProvider provider,
        int limit = 50,
        CancellationToken ct = default)
        => await _context.ErpSyncLogs
            .Where(e => e.TenantId == tenantId && e.Provider == provider && !e.Success)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .AsNoTracking().ToListAsync(ct);
}
