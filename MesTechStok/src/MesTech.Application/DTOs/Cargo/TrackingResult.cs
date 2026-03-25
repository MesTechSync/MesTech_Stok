using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Cargo;

/// <summary>
/// Kargo takip sonucu.
/// </summary>
public sealed class TrackingResult
{
    public string TrackingNumber { get; set; } = string.Empty;
    public CargoStatus Status { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
    public List<TrackingEvent> Events { get; set; } = new();
}

/// <summary>
/// Kargo takip olaylari.
/// </summary>
public sealed class TrackingEvent
{
    public DateTime Timestamp { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CargoStatus Status { get; set; }
}
