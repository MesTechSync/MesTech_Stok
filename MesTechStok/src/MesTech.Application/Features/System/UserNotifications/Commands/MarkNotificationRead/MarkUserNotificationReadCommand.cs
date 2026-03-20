using MediatR;

namespace MesTech.Application.Features.System.UserNotifications.Commands.MarkNotificationRead;

/// <summary>
/// Kullanici ici bildirimi okundu olarak isaretleme komutu.
/// </summary>
public record MarkUserNotificationReadCommand(
    Guid TenantId,
    Guid NotificationId
) : IRequest<bool>;
