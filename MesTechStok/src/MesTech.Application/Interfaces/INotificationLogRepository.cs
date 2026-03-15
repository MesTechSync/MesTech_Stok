using MesTech.Domain.Entities;

namespace MesTech.Application.Interfaces;

/// <summary>
/// NotificationLog veri erisim arayuzu.
/// </summary>
public interface INotificationLogRepository
{
    Task<NotificationLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationLog>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Sayfalanmis bildirim listesi dondurur.
    /// <paramref name="unreadOnly"/> true ise sadece okunmamis (status != Read) bildirimler filtrelenir.
    /// </summary>
    Task<(IReadOnlyList<NotificationLog> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId,
        int page,
        int pageSize,
        bool unreadOnly = false,
        CancellationToken ct = default);

    Task AddAsync(NotificationLog log, CancellationToken ct = default);
    Task UpdateAsync(NotificationLog log, CancellationToken ct = default);
}
