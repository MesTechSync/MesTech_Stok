using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class UserNotificationRepository : IUserNotificationRepository
{
    private readonly AppDbContext _db;

    public UserNotificationRepository(AppDbContext db) => _db = db;

    public async Task<UserNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.UserNotifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken).ConfigureAwait(false);

    public async Task<(IReadOnlyList<UserNotification> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId, Guid userId, int page, int pageSize, bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _db.UserNotifications
            .Where(n => n.TenantId == tenantId && n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, total);
    }

    public async Task<int> GetUnreadCountAsync(Guid tenantId, Guid userId,
        CancellationToken cancellationToken = default)
        => await _db.UserNotifications
            .CountAsync(n => n.TenantId == tenantId && n.UserId == userId && !n.IsRead, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<UserNotification>> GetUnreadByUserAsync(Guid tenantId, Guid userId,
        CancellationToken cancellationToken = default)
        => await _db.UserNotifications
            .Where(n => n.TenantId == tenantId && n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<int> MarkAllAsReadAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
        => await _db.UserNotifications
            .Where(n => n.TenantId == tenantId && n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow)
                .SetProperty(n => n.UpdatedAt, DateTime.UtcNow), cancellationToken).ConfigureAwait(false);

    public async Task AddAsync(UserNotification notification, CancellationToken cancellationToken = default)
        => await _db.UserNotifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);

    public Task UpdateAsync(UserNotification notification, CancellationToken cancellationToken = default)
    {
        _db.UserNotifications.Update(notification);
        return Task.CompletedTask;
    }
}
