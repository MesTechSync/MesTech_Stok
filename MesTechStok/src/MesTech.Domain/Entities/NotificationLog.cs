using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Bildirim kaydi entity'si.
/// Tum kanallardaki (WhatsApp, Telegram, Email, Push, SMS) bildirim gecmisini saklar.
/// </summary>
public sealed class NotificationLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public NotificationChannel Channel { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public string TemplateName { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;
    public DateTime? SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// ORM icin parametresiz constructor.
    /// </summary>
    private NotificationLog() { }

    /// <summary>
    /// Yeni bildirim kaydi olusturur (Pending durumunda).
    /// </summary>
    public static NotificationLog Create(
        Guid tenantId,
        NotificationChannel channel,
        string recipient,
        string templateName,
        string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new NotificationLog
        {
            TenantId = tenantId,
            Channel = channel,
            Recipient = recipient,
            TemplateName = templateName,
            Content = content,
            Status = NotificationStatus.Pending
        };
    }

    /// <summary>
    /// Bildirim gonderildi olarak isaretler.
    /// Gecerli gecis: Pending -> Sent.
    /// </summary>
    public void MarkAsSent()
    {
        if (Status != NotificationStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot mark as sent from {Status} status. Only Pending notifications can be sent.");

        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Bildirim teslim edildi olarak isaretler.
    /// Gecerli gecis: Sent -> Delivered.
    /// </summary>
    public void MarkAsDelivered()
    {
        if (Status != NotificationStatus.Sent)
            throw new InvalidOperationException(
                $"Cannot mark as delivered from {Status} status. Only Sent notifications can be delivered.");

        Status = NotificationStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Bildirim basarisiz olarak isaretler.
    /// Gecerli gecis: Pending | Sent -> Failed.
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status != NotificationStatus.Pending && Status != NotificationStatus.Sent)
            throw new InvalidOperationException(
                $"Cannot mark as failed from {Status} status. Only Pending or Sent notifications can fail.");

        Status = NotificationStatus.Failed;
        ErrorMessage = reason;
    }

    /// <summary>
    /// Bildirim okundu olarak isaretler.
    /// Gecerli gecis: Delivered -> Read.
    /// </summary>
    public void MarkAsRead()
    {
        if (Status != NotificationStatus.Delivered)
            throw new InvalidOperationException(
                $"Cannot mark as read from {Status} status. Only Delivered notifications can be read.");

        Status = NotificationStatus.Read;
        ReadAt = DateTime.UtcNow;
    }

    public override string ToString() =>
        $"[{Channel}] {Recipient} — {TemplateName} ({Status})";
}
