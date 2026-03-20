using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Notifications.Commands.UpdateNotificationSettings;

/// <summary>
/// Bildirim ayarlari guncelleme komutu.
/// Mevcut kaydi gunceller, yoksa yeni kayit olusturur (upsert).
/// </summary>
public record UpdateNotificationSettingsCommand(
    Guid TenantId,
    Guid UserId,
    NotificationChannel Channel,
    string? ChannelAddress,
    bool IsEnabled,
    bool NotifyOnOrderReceived,
    bool NotifyOnLowStock,
    int LowStockThreshold,
    bool NotifyOnInvoiceDue,
    bool NotifyOnPaymentReceived,
    bool NotifyOnPlatformMessage,
    bool NotifyOnAIInsight,
    bool NotifyOnBuyboxLost,
    bool NotifyOnSystemError,
    bool NotifyOnTaxDeadline,
    bool NotifyOnReportReady,
    string? QuietHoursStart,
    string? QuietHoursEnd,
    string PreferredLanguage,
    bool DigestMode,
    string? DigestTime
) : IRequest<Guid>;
