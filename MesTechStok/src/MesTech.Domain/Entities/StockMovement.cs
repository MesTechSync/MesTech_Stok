using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Stok hareketi entity'si.
/// </summary>
public class StockMovement : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    public int NewStockLevel { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }

    // İlişkiler
    public Guid? OrderId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? FromWarehouseId { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public string? FromLocation { get; set; }
    public string? ToLocation { get; set; }

    // Belge
    public string? DocumentNumber { get; set; }
    public string? DocumentUrl { get; set; }

    // Zaman
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? ProcessedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public bool IsApproved { get; set; }

    // Geri alma
    public bool IsReversed { get; set; }
    public Guid? ReversalMovementId { get; set; }

    // Lot/Seri takibi
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    // Barkod tarama
    public string? ScannedBarcode { get; set; }
    public bool IsScannedMovement { get; set; }

    // Concurrency
    public byte[]? RowVersion { get; set; }

    // ── Computed ──

    public bool IsPositiveMovement => Quantity > 0;
    public bool IsNegativeMovement => Quantity < 0;

    public void SetMovementType(StockMovementType type)
    {
        MovementType = type.ToString();
    }

    public override string ToString() => $"{MovementType}: {Quantity} (Product: {ProductId})";
}
