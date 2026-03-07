using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI;

/// <summary>
/// Mock MESA Bot servisi — gercek mesaj gondermez, log'a yazar.
/// Dalga 2'de RealMesaBotClient ile DI'dan swap edilecek.
/// </summary>
public class MockMesaBotService : IMesaBotService
{
    private readonly ILogger<MockMesaBotService> _logger;

    public MockMesaBotService(ILogger<MockMesaBotService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendWhatsAppNotificationAsync(
        string phoneNumber, string templateName,
        Dictionary<string, string> templateData,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] WhatsApp bildirim: {Phone}, sablon: {Template}, veri: {Data}",
            MaskPhone(phoneNumber), templateName,
            string.Join(", ", templateData.Select(kv => $"{kv.Key}={kv.Value}")));

        return Task.FromResult(true);
    }

    public Task<bool> SendTelegramAlertAsync(
        string channelId, string message,
        TelegramAlertLevel level, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] Telegram alert [{Level}]: kanal={Channel}, mesaj={Message}",
            level, channelId, Truncate(message, 100));

        return Task.FromResult(true);
    }

    public Task<bool> SendBulkNotificationAsync(
        NotificationChannel channel, List<string> recipients,
        string templateName, Dictionary<string, string> data,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] Toplu bildirim [{Channel}]: {Count} alici, sablon: {Template}",
            channel, recipients.Count, templateName);

        return Task.FromResult(true);
    }

    private static string MaskPhone(string phone)
    {
        if (phone.Length <= 4) return "****";
        return string.Concat(phone.AsSpan(0, 3), "****", phone.AsSpan(phone.Length - 2));
    }

    private static string Truncate(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength), "...");
    }
}
