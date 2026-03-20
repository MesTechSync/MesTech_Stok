using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Kullanici ici bildirim entity'si.
/// InApp bildirimler (rapor hazir, stok uyarisi, sistem bildirimi vb.) icin kullanilir.
/// BaseEntity'den miras alir (Id, CreatedAt, audit, soft delete).
/// </summary>
public class UserNotification : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationCategory Category { get; private set; }
    public string? ActionUrl { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    // Navigation
    public User? User { get; set; }

    /// <summary>
    /// ORM icin parametresiz constructor.
    /// </summary>
    private UserNotification() { }

    /// <summary>
    /// Yeni kullanici ici bildirim olusturur.
    /// </summary>
    public static UserNotification Create(
        Guid tenantId,
        Guid userId,
        string title,
        string message,
        NotificationCategory category,
        string? actionUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new UserNotification
        {
            TenantId = tenantId,
            UserId = userId,
            Title = title,
            Message = message,
            Category = category,
            ActionUrl = actionUrl,
            IsRead = false,
            ReadAt = null
        };
    }

    /// <summary>
    /// Bildirimi okundu olarak isaretler.
    /// </summary>
    public void MarkAsRead()
    {
        if (IsRead)
            return;

        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }

    public override string ToString() =>
        $"[{Category}] {Title} — {(IsRead ? "Okundu" : "Okunmadi")}";
}
