using MediatR;

namespace MesTech.Application.Features.Notifications.Commands.MarkNotificationRead;

/// <summary>
/// Bildirimi okundu olarak isaretleme komutu.
/// </summary>
public record MarkNotificationReadCommand(Guid TenantId, Guid NotificationId) : IRequest<bool>;
