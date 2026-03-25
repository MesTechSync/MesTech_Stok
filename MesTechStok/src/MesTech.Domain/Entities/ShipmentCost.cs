using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Kargo maliyet kaydi — her gonderim icin kargo ucretini tutar.
/// Zincir 7: Siparis kargolandiginda otomatik gider kaydi olusturur.
/// </summary>
public sealed class ShipmentCost : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; private set; }
    public CargoProvider Provider { get; private set; }
    public decimal Cost { get; private set; }
    public decimal? DesiWeight { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? CargoBarcode { get; private set; }
    public DateTime ShippedAt { get; private set; }
    public bool IsChargedToCustomer { get; private set; }
    public decimal? CustomerChargeAmount { get; private set; }

    private ShipmentCost() { }

    public static ShipmentCost Create(
        Guid tenantId, Guid orderId, CargoProvider provider,
        decimal cost, string? trackingNumber = null,
        decimal? desiWeight = null, bool isChargedToCustomer = false,
        decimal? customerChargeAmount = null)
    {
        if (cost < 0) throw new ArgumentException("Kargo ucreti negatif olamaz.", nameof(cost));

        return new ShipmentCost
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderId = orderId,
            Provider = provider,
            Cost = cost,
            TrackingNumber = trackingNumber,
            DesiWeight = desiWeight,
            ShippedAt = DateTime.UtcNow,
            IsChargedToCustomer = isChargedToCustomer,
            CustomerChargeAmount = customerChargeAmount
        };
    }

    /// <summary>Net kargo gideri (musteri odediyse fark)</summary>
    public decimal NetCost => IsChargedToCustomer
        ? Math.Max(0, Cost - (CustomerChargeAmount ?? 0))
        : Cost;
}
