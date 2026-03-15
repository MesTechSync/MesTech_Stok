namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// MESA Bot bildirim gonderim sonucu consume edilir.
/// Exchange: mestech.mesa.bot.notification.sent.v1
/// </summary>
public record BotNotificationSentEvent
{
    public Guid TenantId { get; init; }
    public string Channel { get; init; } = "";
    public string Recipient { get; init; } = "";
    public string TemplateName { get; init; } = "";
    public string Content { get; init; } = "";
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime SentAt { get; init; }
}
