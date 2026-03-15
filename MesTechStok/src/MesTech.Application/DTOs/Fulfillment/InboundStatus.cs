namespace MesTech.Application.DTOs.Fulfillment;

/// <summary>
/// Inbound sevkiyat durumu.
/// </summary>
public record InboundStatus(
    string ShipmentId,
    string Status,
    int TotalItemsExpected,
    int TotalItemsReceived,
    DateTime? ReceivedAt = null,
    string? TrackingNumber = null
);
