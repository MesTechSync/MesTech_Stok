using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Features.Notifications.Queries.GetNotifications;

/// <summary>
/// Bildirim listesi sorgusu.
/// Sayfalama ve sadece okunmamis filtresi destekler.
/// </summary>
public record GetNotificationsQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 20,
    bool UnreadOnly = false
) : IRequest<NotificationListResult>;
