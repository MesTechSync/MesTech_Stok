using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Envanter lot'u — FEFO (First Expire First Out) yönetimi.
/// </summary>
public class InventoryLot : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal RemainingQty { get; set; }
    public LotStatus Status { get; set; } = LotStatus.Open;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedDate { get; set; }

    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

    public void Consume(decimal quantity)
    {
        if (quantity > RemainingQty)
            throw new InvalidOperationException($"Cannot consume {quantity} from lot {LotNumber}, only {RemainingQty} remaining.");

        RemainingQty -= quantity;
        if (RemainingQty <= 0)
        {
            Status = LotStatus.Closed;
            ClosedDate = DateTime.UtcNow;
        }
    }
}
