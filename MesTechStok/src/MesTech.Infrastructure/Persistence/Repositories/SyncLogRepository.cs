using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class SyncLogRepository : ISyncLogRepository
{
    private readonly AppDbContext _context;

    public SyncLogRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<IReadOnlyList<SyncLog>> GetLatestByPlatformAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        // For each platform, get the most recent sync log
        var latestLogs = await _context.SyncLogs
            .Where(s => s.TenantId == tenantId)
            .GroupBy(s => s.PlatformCode)
            .Select(g => g.OrderByDescending(s => s.StartedAt).First())
            .AsNoTracking()
            .AsNoTracking().ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return latestLogs.AsReadOnly();
    }

    public async Task<IReadOnlyList<SyncLog>> GetFailedSinceAsync(
        Guid tenantId, DateTime since, CancellationToken cancellationToken = default)
        => await _context.SyncLogs
            .Where(s => s.TenantId == tenantId
                     && !s.IsSuccess
                     && s.StartedAt >= since)
            .AsNoTracking()
            .AsNoTracking().ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
