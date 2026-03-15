namespace MesTech.Application.DTOs.Fulfillment;

/// <summary>
/// Fulfillment merkezine inbound sevkiyat olusturma istegi.
/// </summary>
public record InboundShipmentRequest(
    string ShipmentName,
    FulfillmentCenter DestinationCenter,
    IReadOnlyList<InboundItem> Items,
    DateTime? ExpectedArrival = null,
    string? Notes = null
);

/// <summary>
/// Inbound sevkiyattaki tek bir urun kalemi.
/// </summary>
public record InboundItem(
    string SKU,
    int Quantity,
    string? LotNumber = null,
    DateTime? ExpiryDate = null
);
