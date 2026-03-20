using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

/// <summary>
/// Kullanici ici bildirim repository interface'i.
/// </summary>
public interface IUserNotificationRepository
{
    Task<UserNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sayfalanmis kullanici bildirimi listesi dondurur.
    /// <paramref name="unreadOnly"/> true ise sadece okunmamis bildirimler filtrelenir.
    /// </summary>
    Task<(IReadOnlyList<UserNotification> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId,
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanicinin okunmamis bildirim sayisini dondurur.
    /// </summary>
    Task<int> GetUnreadCountAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanicinin tum okunmamis bildirimlerini dondurur.
    /// </summary>
    Task<IReadOnlyList<UserNotification>> GetUnreadByUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(UserNotification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserNotification notification, CancellationToken cancellationToken = default);
}
