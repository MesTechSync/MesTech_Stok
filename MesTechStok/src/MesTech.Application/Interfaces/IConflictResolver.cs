namespace MesTech.Application.Interfaces;

/// <summary>
/// Entegrasyon çakışma çözücü arayüzü.
/// </summary>
public interface IConflictResolver
{
    Task<ConflictResolutionResult> ResolveAsync(SyncConflict conflict, CancellationToken ct = default);
}

public class SyncConflict
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string PlatformCode { get; set; } = string.Empty;
    public string LocalValue { get; set; } = string.Empty;
    public string RemoteValue { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

public class ConflictResolutionResult
{
    public bool IsResolved { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
