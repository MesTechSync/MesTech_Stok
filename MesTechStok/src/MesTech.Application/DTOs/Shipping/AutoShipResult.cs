using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Shipping;

/// <summary>
/// Otomatik kargo gonderim sonucu.
/// </summary>
public sealed class AutoShipResult
{
    public bool Success { get; set; }
    public string? TrackingNumber { get; set; }
    public CargoProvider CargoProvider { get; set; }
    public string? Reason { get; set; }
    public Guid? ShipmentId { get; set; }
    public string? ErrorMessage { get; set; }

    public static AutoShipResult Succeeded(
        string trackingNumber,
        CargoProvider provider,
        Guid shipmentId,
        string reason)
        => new()
        {
            Success = true,
            TrackingNumber = trackingNumber,
            CargoProvider = provider,
            ShipmentId = shipmentId,
            Reason = reason
        };

    public static AutoShipResult Failed(string error, CargoProvider provider = CargoProvider.None)
        => new()
        {
            Success = false,
            ErrorMessage = error,
            CargoProvider = provider
        };
}
