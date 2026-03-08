using MesTech.Domain.Enums;

namespace MesTech.Domain.Models;

/// <summary>
/// Kargo takip sonucu — TrackShipmentAsync donus tipi.
/// </summary>
public class TrackingResult
{
    public string TrackingNumber { get; set; } = string.Empty;
    public CargoStatus Status { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
    public List<TrackingEvent> Events { get; set; } = new();
}

/// <summary>
/// Kargo takip olayi — gonderinin gecmisteki durum degisiklikleri.
/// </summary>
public class TrackingEvent
{
    public DateTime Timestamp { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CargoStatus Status { get; set; }
}
