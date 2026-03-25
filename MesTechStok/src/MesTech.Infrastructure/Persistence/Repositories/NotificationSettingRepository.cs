using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class NotificationSettingRepository : INotificationSettingRepository
{
    private readonly AppDbContext _context;

    public NotificationSettingRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<NotificationSetting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<NotificationSetting>()
            .AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<NotificationSetting>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.Set<NotificationSetting>()
            .Where(s => s.UserId == userId)
            .AsNoTracking().ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<NotificationSetting?> GetByUserAndChannelAsync(Guid userId, NotificationChannel channel, CancellationToken cancellationToken = default)
        => await _context.Set<NotificationSetting>()
            .AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId && s.Channel == channel, cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(NotificationSetting setting, CancellationToken cancellationToken = default)
        => await _context.Set<NotificationSetting>().AddAsync(setting, cancellationToken).ConfigureAwait(false);

    public Task UpdateAsync(NotificationSetting setting, CancellationToken cancellationToken = default)
    {
        _context.Set<NotificationSetting>().Update(setting);
        return Task.CompletedTask;
    }
}
