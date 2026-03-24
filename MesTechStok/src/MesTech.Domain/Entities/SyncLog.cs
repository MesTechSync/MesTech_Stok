using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities;

/// <summary>
/// Entegrasyon senkronizasyon log'u.
/// Platform-agnostic — tenant filter UYGULANMAZ.
/// Admin IgnoreQueryFilters() olmadan tüm logları görebilir.
/// TenantId veri sahipliği için tutulur ama otomatik filtrelenmez.
/// </summary>
public class SyncLog : BaseEntity
{
    /// <summary>Veri sahipliği için — query filter'da kullanılmaz.</summary>
    public Guid TenantId { get; set; }
    public string PlatformCode { get; set; } = string.Empty;
    public SyncDirection Direction { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.NotSynced;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int ItemsProcessed { get; set; }
    public int ItemsFailed { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? CorrelationId { get; set; }

    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;

    /// <summary>Sync başlatıldığında çağrılır.</summary>
    public void MarkAsStarted()
    {
        SyncStatus = Enums.SyncStatus.NotSynced;
        StartedAt = DateTime.UtcNow;
        RaiseDomainEvent(new SyncRequestedEvent(TenantId, PlatformCode, Direction, EntityType, EntityId, DateTime.UtcNow));
    }

    /// <summary>Sync hata ile bittiğinde çağrılır.</summary>
    public void MarkAsFailed(string errorMessage)
    {
        IsSuccess = false;
        ErrorMessage = errorMessage;
        SyncStatus = SyncStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        RaiseDomainEvent(new SyncErrorOccurredEvent(TenantId, PlatformCode, "SyncFailure", errorMessage, DateTime.UtcNow));
    }

    /// <summary>Sync başarılı bittiğinde çağrılır.</summary>
    public void MarkAsCompleted(int processed, int failed)
    {
        IsSuccess = failed == 0;
        ItemsProcessed = processed;
        ItemsFailed = failed;
        SyncStatus = SyncStatus.Synced;
        CompletedAt = DateTime.UtcNow;
    }
}
