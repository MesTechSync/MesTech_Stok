using MediatR;

namespace MesTech.Application.Features.System.UserNotifications.Queries.GetUnreadNotificationCount;

/// <summary>
/// Kullanicinin okunmamis bildirim sayisi sorgusu.
/// </summary>
public record GetUnreadNotificationCountQuery(
    Guid TenantId,
    Guid UserId
) : IRequest<int>;
