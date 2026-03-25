using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// KVKK/GDPR uyumluluk denetim kaydı — her kişisel veri işlemi loglanır.
/// Yasal zorunluluk: 6698 sayılı KVKK madde 12, GDPR Article 30.
/// Bu kayıtlar SİLİNEMEZ — yasal saklama süresi 10 yıl.
/// </summary>
public sealed class KvkkAuditLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid RequestedByUserId { get; private set; }
    public KvkkOperationType OperationType { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public int AffectedRecordCount { get; private set; }
    public string? Details { get; private set; }
    public DateTime CompletedAt { get; private set; }
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }

    private KvkkAuditLog() { }

    public static KvkkAuditLog Create(
        Guid tenantId,
        Guid requestedByUserId,
        KvkkOperationType operationType,
        string reason,
        int affectedRecordCount,
        bool isSuccess,
        string? details = null,
        string? errorMessage = null)
    {
        return new KvkkAuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RequestedByUserId = requestedByUserId,
            OperationType = operationType,
            Reason = reason,
            AffectedRecordCount = affectedRecordCount,
            IsSuccess = isSuccess,
            Details = details,
            ErrorMessage = errorMessage,
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum KvkkOperationType
{
    DataExport = 1,
    DataDeletion = 2,
    DataAnonymization = 3,
    ConsentRecorded = 4,
    ConsentWithdrawn = 5,
    DataBreach = 6
}
