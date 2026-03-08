namespace MesTech.Domain.Models;

/// <summary>
/// Kargo gonderim sonucu — CreateShipmentAsync donus tipi.
/// </summary>
public class ShipmentResult
{
    public bool Success { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ShipmentId { get; set; }
    public string? LabelUrl { get; set; }
    public string? ErrorMessage { get; set; }

    public static ShipmentResult Failed(string error)
        => new() { Success = false, ErrorMessage = error };

    public static ShipmentResult Succeeded(string trackingNumber, string shipmentId, string? labelUrl = null)
        => new() { Success = true, TrackingNumber = trackingNumber, ShipmentId = shipmentId, LabelUrl = labelUrl };
}
