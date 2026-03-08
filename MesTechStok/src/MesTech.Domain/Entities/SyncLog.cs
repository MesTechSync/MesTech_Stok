using MesTech.Domain.Common;
using MesTech.Domain.Enums;

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
}
