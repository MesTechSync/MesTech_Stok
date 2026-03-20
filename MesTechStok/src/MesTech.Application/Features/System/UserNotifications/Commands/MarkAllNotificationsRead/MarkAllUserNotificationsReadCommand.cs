using MediatR;

namespace MesTech.Application.Features.System.UserNotifications.Commands.MarkAllNotificationsRead;

/// <summary>
/// Kullanicinin tum bildirimlerini okundu olarak isaretleme komutu.
/// </summary>
public record MarkAllUserNotificationsReadCommand(
    Guid TenantId,
    Guid UserId
) : IRequest<int>;
