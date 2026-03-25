using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Platform;

/// <summary>
/// Platform Card data transfer object.
/// </summary>
public sealed class PlatformCardDto
{
    public PlatformType Platform { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LogoColor { get; set; } = string.Empty;
    public bool AdapterAvailable { get; set; }
    public int StoreCount { get; set; }
    public int ActiveStoreCount { get; set; }
    public string? WorstStatus { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string AuthType { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
}
