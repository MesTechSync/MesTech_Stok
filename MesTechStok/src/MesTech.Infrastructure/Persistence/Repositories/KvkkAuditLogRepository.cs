using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class KvkkAuditLogRepository : IKvkkAuditLogRepository
{
    private readonly AppDbContext _context;

    public KvkkAuditLogRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task AddAsync(KvkkAuditLog log, CancellationToken ct = default)
        => await _context.Set<KvkkAuditLog>().AddAsync(log, ct).ConfigureAwait(false);

    public async Task<(IReadOnlyList<KvkkAuditLog> Items, int TotalCount)> GetByTenantPagedAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Set<KvkkAuditLog>()
            .Where(l => l.TenantId == tenantId);

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(l => l.CompletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, total);
    }
}
