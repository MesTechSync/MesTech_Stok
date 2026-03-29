using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Fulfillment gonderi kaydi (Amazon FBA / Hepsilojistik).
/// G383: GetFulfillmentShipments stub→real data.
/// </summary>
public sealed class FulfillmentShipment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string TrackingNumber { get; private set; } = string.Empty;
    public string Center { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Pending";
    public int ItemCount { get; private set; }
    public Guid? OrderId { get; private set; }
    public string? CarrierCode { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    private FulfillmentShipment() { }

    public static FulfillmentShipment Create(
        Guid tenantId, string trackingNumber, string center, int itemCount, Guid? orderId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(center);
        return new FulfillmentShipment
        {
            TenantId = tenantId,
            TrackingNumber = trackingNumber,
            Center = center,
            ItemCount = itemCount,
            OrderId = orderId,
            Status = "Created"
        };
    }

    public void MarkShipped(string? carrierCode = null)
    {
        Status = "Shipped";
        CarrierCode = carrierCode;
        ShippedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDelivered()
    {
        Status = "Delivered";
        DeliveredAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCancelled()
    {
        Status = "Cancelled";
        UpdatedAt = DateTime.UtcNow;
    }
}
