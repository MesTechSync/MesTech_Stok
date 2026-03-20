using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

/// <summary>
/// Bildirim ayarlari repository interface'i.
/// </summary>
public interface INotificationSettingRepository
{
    Task<NotificationSetting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationSetting>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationSetting?> GetByUserAndChannelAsync(Guid userId, NotificationChannel channel, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationSetting setting, CancellationToken cancellationToken = default);
    Task UpdateAsync(NotificationSetting setting, CancellationToken cancellationToken = default);
}
