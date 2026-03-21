using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Platform;

/// <summary>
/// Platform Sync Status data transfer object.
/// </summary>
public class PlatformSyncStatusDto
{
    public PlatformType Platform { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public int StoreCount { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public string? LastError { get; set; }
    public int ErrorCountToday { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public string HealthColor { get; set; } = string.Empty;
}
