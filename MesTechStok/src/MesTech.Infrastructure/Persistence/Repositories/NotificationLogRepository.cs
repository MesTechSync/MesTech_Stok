using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class NotificationLogRepository : INotificationLogRepository
{
    private readonly AppDbContext _context;

    public NotificationLogRepository(AppDbContext context) => _context = context;

    public async Task<NotificationLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.NotificationLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IReadOnlyList<NotificationLog>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.NotificationLogs
            .Where(n => n.TenantId == tenantId)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<NotificationLog> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId,
        int page,
        int pageSize,
        bool unreadOnly = false,
        CancellationToken ct = default)
    {
        var query = _context.NotificationLogs
            .Where(n => n.TenantId == tenantId);

        if (unreadOnly)
            query = query.Where(n => n.ReadAt == null);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(NotificationLog log, CancellationToken ct = default)
        => await _context.NotificationLogs.AddAsync(log, ct);

    public Task UpdateAsync(NotificationLog log, CancellationToken ct = default)
    {
        _context.NotificationLogs.Update(log);
        return Task.CompletedTask;
    }
}
