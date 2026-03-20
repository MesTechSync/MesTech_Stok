using MediatR;

namespace MesTech.Application.Features.System.UserNotifications.Queries.GetUserNotifications;

/// <summary>
/// Kullanici ici bildirim listesi sorgusu.
/// Sayfalama, userId ve sadece okunmamis filtresi destekler.
/// </summary>
public record GetUserNotificationsQuery(
    Guid TenantId,
    Guid UserId,
    int Page = 1,
    int PageSize = 20,
    bool UnreadOnly = false
) : IRequest<UserNotificationListResult>;
