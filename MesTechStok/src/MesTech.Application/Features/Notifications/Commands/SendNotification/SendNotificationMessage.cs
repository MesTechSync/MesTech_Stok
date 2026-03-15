namespace MesTech.Application.Features.Notifications.Commands.SendNotification;

/// <summary>
/// MESA Bot'a gonderilecek bildirim istegi mesaji.
/// Bot bu mesaji consume edip gercek bildirim gonderimini yapar,
/// sonra BotNotificationSentEvent ile sonucu bildirir.
/// </summary>
public record SendNotificationMessage
{
    public Guid NotificationLogId { get; init; }
    public Guid TenantId { get; init; }
    public string Channel { get; init; } = "";
    public string Recipient { get; init; } = "";
    public string TemplateName { get; init; } = "";
    public string Content { get; init; } = "";
    public DateTime RequestedAt { get; init; }
}
