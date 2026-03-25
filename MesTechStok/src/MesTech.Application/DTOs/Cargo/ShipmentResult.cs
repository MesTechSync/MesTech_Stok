namespace MesTech.Application.DTOs.Cargo;

/// <summary>
/// Kargo gonderi sonucu.
/// </summary>
public sealed class ShipmentResult
{
    public bool Success { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ShipmentId { get; set; }
    public string? LabelUrl { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>Kargo ücreti (TRY). Zincir 7: gider yevmiye kaydı tetikler.</summary>
    public decimal ShippingCost { get; set; }

    public static ShipmentResult Failed(string error)
        => new() { Success = false, ErrorMessage = error };

    public static ShipmentResult Succeeded(string trackingNumber, string shipmentId, string? labelUrl = null, decimal shippingCost = 0)
        => new() { Success = true, TrackingNumber = trackingNumber, ShipmentId = shipmentId, LabelUrl = labelUrl, ShippingCost = shippingCost };
}
