using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities;

/// <summary>
/// Kullanici bildirim tercihleri entity'si.
/// Her kullanici-kanal cifti icin bir kayit olusturulur.
/// ChannelAddress PII icerdigi icin log'a YAZILMAZ.
/// </summary>
public class NotificationSetting : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Kanal adresi (e-posta, telefon, Telegram chat ID vb.).
    /// PII — log'a yazilmaz.
    /// </summary>
    public string? ChannelAddress { get; set; }

    public bool IsEnabled { get; set; } = true;

    // Category-based notification preferences
    public bool NotifyOnOrderReceived { get; set; } = true;
    public bool NotifyOnLowStock { get; set; } = true;
    public int LowStockThreshold { get; set; } = 5;
    public bool NotifyOnInvoiceDue { get; set; } = true;
    public bool NotifyOnPaymentReceived { get; set; } = true;
    public bool NotifyOnPlatformMessage { get; set; } = true;
    public bool NotifyOnAIInsight { get; set; }
    public bool NotifyOnBuyboxLost { get; set; } = true;
    public bool NotifyOnSystemError { get; set; } = true;
    public bool NotifyOnTaxDeadline { get; set; } = true;
    public bool NotifyOnReportReady { get; set; } = true;

    // Quiet hours
    public TimeOnly? QuietHoursStart { get; set; }
    public TimeOnly? QuietHoursEnd { get; set; }

    // User preferences
    public string PreferredLanguage { get; set; } = "tr";
    public bool DigestMode { get; set; }
    public TimeOnly? DigestTime { get; set; }

    // Navigation
    public User User { get; set; } = null!;

    /// <summary>
    /// Belirtilen kategori ve zaman icin bildirim gonderilmeli mi?
    /// Quiet hours ve kategori bazli tercih kontrolu yapar.
    /// </summary>
    public bool ShouldNotify(NotificationCategory category, DateTime? atTime = null)
    {
        if (!IsEnabled)
            return false;

        // Quiet hours check
        if (QuietHoursStart.HasValue && QuietHoursEnd.HasValue)
        {
            var now = atTime.HasValue
                ? TimeOnly.FromDateTime(atTime.Value)
                : TimeOnly.FromDateTime(DateTime.UtcNow);

            if (IsInQuietHours(now))
                return false;
        }

        return category switch
        {
            NotificationCategory.Order => NotifyOnOrderReceived,
            NotificationCategory.Stock => NotifyOnLowStock,
            NotificationCategory.Invoice => NotifyOnInvoiceDue,
            NotificationCategory.Payment => NotifyOnPaymentReceived,
            NotificationCategory.System => NotifyOnSystemError,
            NotificationCategory.CRM => NotifyOnPlatformMessage,
            NotificationCategory.AI => NotifyOnAIInsight,
            NotificationCategory.Report => NotifyOnReportReady,
            NotificationCategory.Tax => NotifyOnTaxDeadline,
            NotificationCategory.Buybox => NotifyOnBuyboxLost,
            _ => false
        };
    }

    /// <summary>
    /// Bu kanal icin adres bilgisi zorunlu mu?
    /// </summary>
    public bool RequiresChannelAddress()
    {
        return Channel is NotificationChannel.Email
            or NotificationChannel.Telegram
            or NotificationChannel.WhatsApp
            or NotificationChannel.SMS;
    }

    /// <summary>
    /// Ayarlari gunceller ve domain event firlatir.
    /// </summary>
    public void MarkUpdated()
    {
        RaiseDomainEvent(new NotificationSettingsUpdatedEvent(
            UserId, Channel, IsEnabled, DateTime.UtcNow));
    }

    private bool IsInQuietHours(TimeOnly now)
    {
        var start = QuietHoursStart!.Value;
        var end = QuietHoursEnd!.Value;

        // Handle overnight quiet hours (e.g., 23:00 - 07:00)
        if (start > end)
            return now >= start || now <= end;

        return now >= start && now <= end;
    }
}
