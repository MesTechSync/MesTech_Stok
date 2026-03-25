using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Shipping;

/// <summary>
/// Kargo gonderi durum bilgisi.
/// </summary>
public sealed class ShipmentStatusDto
{
    public string TrackingNumber { get; set; } = string.Empty;
    public CargoProvider Provider { get; set; }
    public CargoStatus Status { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
    public List<ShipmentEventDto> Events { get; set; } = new();
}

/// <summary>
/// Kargo takip olayi.
/// </summary>
public sealed class ShipmentEventDto
{
    public DateTime Timestamp { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CargoStatus Status { get; set; }
}
