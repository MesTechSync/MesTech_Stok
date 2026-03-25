using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Denetim kaydi entity'si.
/// BaseEntity'den miras ALMAZ — kendisi denetim kaydinin ta kendisidir.
/// Tum entity degisikliklerini (CRUD) JSON diff olarak saklar.
/// </summary>
public sealed class AuditLog : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? IpAddress { get; private set; }

    /// <summary>
    /// ORM icin parametresiz constructor.
    /// </summary>
    private AuditLog() { }

    /// <summary>
    /// Yeni denetim kaydi olusturur.
    /// </summary>
    public static AuditLog Create(
        Guid tenantId,
        Guid? userId,
        string userName,
        string action,
        string entityType,
        Guid? entityId,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress
        };
    }

    public override string ToString() =>
        $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {UserName} {Action} {EntityType} ({EntityId})";
}
