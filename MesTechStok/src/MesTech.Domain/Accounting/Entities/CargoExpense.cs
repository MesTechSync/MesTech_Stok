using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Kargo gider kaydi — siparis bazinda kargo maliyeti.
/// </summary>
public sealed class CargoExpense : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string? OrderId { get; private set; }
    public string CarrierName { get; private set; } = string.Empty;
    public string? TrackingNumber { get; private set; }
    public decimal Cost { get; private set; }
    public bool IsBilled { get; private set; }
    public DateTime? BilledAt { get; private set; }

    private CargoExpense() { }

    public static CargoExpense Create(
        Guid tenantId,
        string carrierName,
        decimal cost,
        string? orderId = null,
        string? trackingNumber = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(carrierName);
        if (cost < 0)
            throw new ArgumentOutOfRangeException(nameof(cost), "Cost must be non-negative.");

        return new CargoExpense
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderId = orderId,
            CarrierName = carrierName,
            TrackingNumber = trackingNumber,
            Cost = cost,
            IsBilled = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsBilled()
    {
        IsBilled = true;
        BilledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
