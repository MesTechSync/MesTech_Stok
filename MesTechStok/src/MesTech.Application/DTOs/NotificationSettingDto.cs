namespace MesTech.Application.DTOs;

/// <summary>
/// Bildirim ayarlari DTO'su — kullanici tercihlerini UI'a tasir.
/// ChannelAddress PII oldugu icin bu DTO'da yer ALMAZ.
/// </summary>
public class NotificationSettingDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool NotifyOnOrderReceived { get; set; }
    public bool NotifyOnLowStock { get; set; }
    public int LowStockThreshold { get; set; }
    public bool NotifyOnInvoiceDue { get; set; }
    public bool NotifyOnPaymentReceived { get; set; }
    public bool NotifyOnPlatformMessage { get; set; }
    public bool NotifyOnAIInsight { get; set; }
    public bool NotifyOnBuyboxLost { get; set; }
    public bool NotifyOnSystemError { get; set; }
    public bool NotifyOnTaxDeadline { get; set; }
    public bool NotifyOnReportReady { get; set; }
    public string? QuietHoursStart { get; set; }
    public string? QuietHoursEnd { get; set; }
    public string PreferredLanguage { get; set; } = "tr";
    public bool DigestMode { get; set; }
    public string? DigestTime { get; set; }
}
