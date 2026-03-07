namespace MesTech.Application.Interfaces;

/// <summary>
/// MESA OS Bot servis kontrati (WhatsApp + Telegram).
/// Dalga 1: MockMesaBotService (log'a yazar).
/// Dalga 2+: RealMesaBotClient (HTTP → MESA OS Bot Engine port 5902).
/// </summary>
public interface IMesaBotService
{
    /// <summary>WhatsApp bildirim gonderir (template bazli).</summary>
    Task<bool> SendWhatsAppNotificationAsync(
        string phoneNumber,
        string templateName,
        Dictionary<string, string> templateData,
        CancellationToken ct = default);

    /// <summary>Telegram kanal/gruba alert gonderir.</summary>
    Task<bool> SendTelegramAlertAsync(
        string channelId,
        string message,
        TelegramAlertLevel level = TelegramAlertLevel.Info,
        CancellationToken ct = default);

    /// <summary>Toplu bildirim gonderir (birden fazla alici).</summary>
    Task<bool> SendBulkNotificationAsync(
        NotificationChannel channel,
        List<string> recipients,
        string templateName,
        Dictionary<string, string> data,
        CancellationToken ct = default);
}

/// <summary>Telegram alert seviyeleri.</summary>
public enum TelegramAlertLevel
{
    Info,
    Warning,
    Critical
}

/// <summary>Bildirim kanali.</summary>
public enum NotificationChannel
{
    WhatsApp,
    Telegram
}
