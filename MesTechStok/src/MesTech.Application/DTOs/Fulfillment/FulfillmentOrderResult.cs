namespace MesTech.Application.DTOs.Fulfillment;

/// <summary>
/// Fulfillment merkezinden gonderilen siparis bilgisi.
/// </summary>
public record FulfillmentOrderResult(
    string OrderId,
    string Status,
    IReadOnlyList<FulfillmentOrderItem> Items,
    DateTime? ShippedDate = null,
    string? TrackingNumber = null,
    string? CarrierName = null
);

/// <summary>
/// Fulfillment siparisindeki tek bir urun kalemi.
/// </summary>
public record FulfillmentOrderItem(
    string SKU,
    int QuantityOrdered,
    int QuantityShipped
);
