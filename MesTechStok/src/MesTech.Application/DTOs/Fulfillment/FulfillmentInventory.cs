namespace MesTech.Application.DTOs.Fulfillment;

/// <summary>
/// Fulfillment merkezindeki envanter bilgisi — birden fazla SKU icin toplu sonuc.
/// </summary>
public record FulfillmentInventory(
    FulfillmentCenter Center,
    IReadOnlyList<FulfillmentStock> Stocks,
    DateTime QueriedAt
);

/// <summary>
/// Tek bir SKU icin fulfillment merkezi stok bilgisi.
/// </summary>
public record FulfillmentStock(
    string SKU,
    int AvailableQuantity,
    int ReservedQuantity,
    int InboundQuantity
);
