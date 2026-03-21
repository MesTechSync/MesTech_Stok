using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core backed user notification repository.
/// Uses Set&lt;UserNotification&gt;() since no explicit DbSet exists yet.
/// </summary>
public sealed class UserNotificationRepository : IUserNotificationRepository
{
    private readonly AppDbContext _dbContext;

    public UserNotificationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserNotification>()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<UserNotification> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId,
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<UserNotification>()
            .Where(n => n.TenantId == tenantId && n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserNotification>()
            .CountAsync(n => n.TenantId == tenantId && n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task<IReadOnlyList<UserNotification>> GetUnreadByUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserNotification>()
            .Where(n => n.TenantId == tenantId && n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserNotification notification, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<UserNotification>().AddAsync(notification, cancellationToken);
    }

    public Task UpdateAsync(UserNotification notification, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<UserNotification>().Update(notification);
        return Task.CompletedTask;
    }
}
