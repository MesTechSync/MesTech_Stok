using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI;

/// <summary>
/// Mock MESA Bot servisi — gercek mesaj gondermez, log'a yazar.
/// Template bazli bildirim uretimi: siparis, stok, kargo.
/// Dalga 2+: RealMesaBotClient ile DI'dan swap edilecek.
/// </summary>
public class MockMesaBotService : IMesaBotService
{
    private readonly ILogger<MockMesaBotService> _logger;

    private static readonly Dictionary<string, string> Templates = new()
    {
        ["order_received"] =
            "Sayin {customerName}, siparisginiz alindi! " +
            "Siparis No: #{orderNumber} — Toplam: {totalAmount} TL. " +
            "Hazirlaniyor, kargoya verildiginde bilgilendirileceksiniz.",

        ["stock_low"] =
            "STOK UYARISI: {sku} ({productName}) stok kritik seviyede! " +
            "Mevcut: {current} adet, Minimum: {min} adet. " +
            "Acil tedarik gerekli.",

        ["cargo_shipped"] =
            "Sayin {customerName}, kargonuz yola cikti! " +
            "Siparis No: #{orderNumber} — Kargo Firmasi: {cargoCompany} " +
            "Takip No: {trackingCode}. Teslimat suresi: 1-3 is gunu.",

        ["daily_summary"] =
            "Gunluk Ozet ({date}): " +
            "{orderCount} yeni siparis, {totalRevenue} TL ciro, " +
            "{lowStockCount} urun stok alarmi.",

        ["welcome"] =
            "MesTech'e hosgeldiniz! Magaza {storeName} basariyla baglandi. " +
            "Platform: {platform}. Stok senkronizasyonu aktif."
    };

    public MockMesaBotService(ILogger<MockMesaBotService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendWhatsAppNotificationAsync(
        string phoneNumber, string templateName,
        Dictionary<string, string> templateData,
        CancellationToken ct = default)
    {
        var message = RenderTemplate(templateName, templateData);

        _logger.LogInformation(
            "[MOCK] WhatsApp -> {Phone} | sablon: {Template} | mesaj: {Message}",
            MaskPhone(phoneNumber), templateName, Truncate(message, 120));

        return Task.FromResult(true);
    }

    public Task<bool> SendTelegramAlertAsync(
        string channelId, string message,
        TelegramAlertLevel level, CancellationToken ct = default)
    {
        var emoji = level switch
        {
            TelegramAlertLevel.Warning => "[!]",
            TelegramAlertLevel.Critical => "[!!!]",
            _ => "[i]"
        };

        _logger.LogInformation(
            "[MOCK] Telegram {Emoji} -> kanal={Channel} | {Message}",
            emoji, channelId, Truncate(message, 120));

        return Task.FromResult(true);
    }

    public Task<bool> SendBulkNotificationAsync(
        NotificationChannel channel, List<string> recipients,
        string templateName, Dictionary<string, string> data,
        CancellationToken ct = default)
    {
        var message = RenderTemplate(templateName, data);

        _logger.LogInformation(
            "[MOCK] Toplu {Channel} -> {Count} alici | sablon: {Template} | mesaj: {Message}",
            channel, recipients.Count, templateName, Truncate(message, 80));

        return Task.FromResult(true);
    }

    // ── Template rendering ──

    internal static string RenderTemplate(
        string templateName, Dictionary<string, string> data)
    {
        if (!Templates.TryGetValue(templateName, out var template))
            return $"[Bilinmeyen sablon: {templateName}] " +
                   string.Join(", ", data.Select(kv => $"{kv.Key}={kv.Value}"));

        var result = template;
        foreach (var kv in data)
            result = result.Replace($"{{{kv.Key}}}", kv.Value);

        return result;
    }

    private static string MaskPhone(string phone)
    {
        if (phone.Length <= 4) return "****";
        return string.Concat(phone.AsSpan(0, 3), "****", phone.AsSpan(phone.Length - 2));
    }

    private static string Truncate(string text, int maxLength)
    {
        return text.Length <= maxLength
            ? text
            : string.Concat(text.AsSpan(0, maxLength), "...");
    }
}
