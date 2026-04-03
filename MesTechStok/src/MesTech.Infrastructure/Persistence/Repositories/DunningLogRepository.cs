using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class DunningLogRepository : IDunningLogRepository
{
    private readonly AppDbContext _context;

    public DunningLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DunningLog>> GetBySubscriptionIdAsync(Guid subscriptionId, CancellationToken ct = default)
        => await _context.DunningLogs
            .Where(d => d.TenantSubscriptionId == subscriptionId)
            .OrderByDescending(d => d.AttemptDate)
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<int> GetAttemptCountAsync(Guid subscriptionId, CancellationToken ct = default)
        => await _context.DunningLogs
            .CountAsync(d => d.TenantSubscriptionId == subscriptionId, ct).ConfigureAwait(false);

    public async Task AddAsync(DunningLog log, CancellationToken ct = default)
        => await _context.DunningLogs.AddAsync(log, ct).ConfigureAwait(false);
}
