using MediatR;

namespace MesTech.Application.Features.Notifications.Commands.SendNotification;

/// <summary>
/// Bildirim gonderme komutu.
/// NotificationLog olusturur ve MESA Bot'a RabbitMQ uzerinden iletir.
/// </summary>
public record SendNotificationCommand(
    Guid TenantId,
    string Channel,
    string Recipient,
    string TemplateName,
    string Content
) : IRequest<Guid>;
