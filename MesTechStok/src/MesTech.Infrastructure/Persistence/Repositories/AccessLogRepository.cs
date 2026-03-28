using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class AccessLogRepository : IAccessLogRepository
{
    private readonly AppDbContext _context;

    public AccessLogRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<IReadOnlyList<AccessLog>> GetPagedAsync(
        Guid tenantId,
        DateTime? from,
        DateTime? to,
        Guid? userId,
        string? action,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.AccessLogs
            .Where(a => a.TenantId == tenantId)
            .AsNoTracking();

        if (from.HasValue)
            query = query.Where(a => a.AccessTime >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.AccessTime <= to.Value);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        return await query
            .OrderByDescending(a => a.AccessTime)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(AccessLog log, CancellationToken ct = default)
    {
        await _context.AccessLogs.AddAsync(log, ct).ConfigureAwait(false);
    }
}
