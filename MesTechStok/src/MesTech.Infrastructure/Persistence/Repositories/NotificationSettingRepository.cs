using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core backed notification setting repository.
/// Uses Set&lt;NotificationSetting&gt;() since no explicit DbSet exists yet.
/// </summary>
public sealed class NotificationSettingRepository : INotificationSettingRepository
{
    private readonly AppDbContext _dbContext;

    public NotificationSettingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NotificationSetting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<NotificationSetting>()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationSetting>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<NotificationSetting>()
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificationSetting?> GetByUserAndChannelAsync(Guid userId, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<NotificationSetting>()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Channel == channel, cancellationToken);
    }

    public async Task AddAsync(NotificationSetting setting, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<NotificationSetting>().AddAsync(setting, cancellationToken);
    }

    public Task UpdateAsync(NotificationSetting setting, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<NotificationSetting>().Update(setting);
        return Task.CompletedTask;
    }
}
