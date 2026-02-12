using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models;

/// <summary>
/// Stok hareketlerini kaydetmek için kullanılan model
/// Giriş, çıkış, satış, iade vb. tüm stok hareketlerini takip eder
/// </summary>
public class StockMovement
{
    [Key]
    public int Id { get; set; }

    // Product Reference
    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    // Movement Details
    public int Quantity { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    public int NewStockLevel { get; set; }

    [Required]
    [MaxLength(50)]
    public string MovementType { get; set; } = string.Empty; // IN, OUT, ADJUSTMENT, TRANSFER, RETURN, LOSS, FOUND

    [MaxLength(100)]
    public string? Reason { get; set; }

    [MaxLength(200)]
    public string? Notes { get; set; }

    // Pricing at time of movement
    [Column(TypeName = "decimal(18,2)")]
    public decimal? UnitCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalCost { get; set; }

    // References
    public int? OrderId { get; set; }
    public virtual Order? Order { get; set; }

    public int? SupplierId { get; set; }
    public virtual Supplier? Supplier { get; set; }

    public int? CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }

    // Location tracking
    public int? FromWarehouseId { get; set; }
    [NotMapped]
    public virtual Warehouse? FromWarehouse { get; set; }

    public int? ToWarehouseId { get; set; }
    [NotMapped]
    public virtual Warehouse? ToWarehouse { get; set; }

    [MaxLength(50)]
    public string? FromLocation { get; set; }

    [MaxLength(50)]
    public string? ToLocation { get; set; }

    // Document References
    [MaxLength(50)]
    public string? DocumentNumber { get; set; }

    [MaxLength(255)]
    public string? DocumentUrl { get; set; }

    // Tracking
    public DateTime Date { get; set; } = DateTime.Now;

    // Test compatibility alias
    [NotMapped]
    public DateTime CreatedAt { get => Date; set => Date = value; }

    // ALPHA TEAM: Added properties for IStockService compatibility
    [NotMapped]
    public int ChangeAmount { get => Quantity; set => Quantity = value; }

    [NotMapped]
    public string? CreatedBy { get => ProcessedBy; set => ProcessedBy = value; }

    // Helper method for enum-string conversion in tests
    public void SetMovementType(StockMovementType movementType)
    {
        MovementType = movementType switch
        {
            StockMovementType.In => "IN",
            StockMovementType.Out => "OUT",
            StockMovementType.StockIn => "IN",
            StockMovementType.StockOut => "OUT",
            _ => movementType.ToString()
        };
    }

    [MaxLength(50)]
    public string? ProcessedBy { get; set; }

    [MaxLength(50)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    // Status
    public bool IsApproved { get; set; } = true;
    public bool IsReversed { get; set; } = false;
    public int? ReversalMovementId { get; set; }

    // Batch/Serial tracking
    [MaxLength(50)]
    public string? BatchNumber { get; set; }

    [MaxLength(50)]
    public string? SerialNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }

    // Barcode tracking
    [MaxLength(50)]
    public string? ScannedBarcode { get; set; }

    public bool IsScannedMovement { get; set; } = false;

    // Calculated Properties
    [NotMapped]
    public string MovementTypeDescription
    {
        get
        {
            return MovementType?.ToUpper() switch
            {
                "IN" => "Stok Girişi",
                "OUT" => "Stok Çıkışı",
                "ADJUSTMENT" => "Stok Düzeltmesi",
                "TRANSFER" => "Transfer",
                "RETURN" => "İade",
                "LOSS" => "Fire/Kayıp",
                "FOUND" => "Bulundu/Fazla",
                "SALE" => "Satış",
                "PURCHASE" => "Satın Alma",
                "PRODUCTION" => "Üretim",
                "CONSUMPTION" => "Tüketim",
                _ => MovementType ?? "Bilinmeyen"
            };
        }
    }

    [NotMapped]
    public string MovementTypeColor
    {
        get
        {
            return MovementType?.ToUpper() switch
            {
                "IN" or "PURCHASE" or "PRODUCTION" or "FOUND" or "RETURN" => "#4CAF50", // Green
                "OUT" or "SALE" or "CONSUMPTION" or "LOSS" => "#F44336", // Red
                "ADJUSTMENT" => "#FF9800", // Orange
                "TRANSFER" => "#2196F3", // Blue
                _ => "#9E9E9E" // Gray
            };
        }
    }

    [NotMapped]
    public bool IsPositiveMovement =>
        MovementType?.ToUpper() is "IN" or "PURCHASE" or "PRODUCTION" or "FOUND" or "RETURN";

    [NotMapped]
    public bool IsNegativeMovement =>
        MovementType?.ToUpper() is "OUT" or "SALE" or "CONSUMPTION" or "LOSS";

    [NotMapped]
    public string DisplayText => $"{MovementTypeDescription}: {(Quantity >= 0 ? "+" : "")}{Quantity}";

    public override string ToString() =>
        $"{Date:dd.MM.yyyy HH:mm} - {Product?.Name}: {DisplayText}";

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Stok hareketi türlerini belirten enum
/// </summary>
public enum StockMovementType
{
    /// <summary>Stok girişi (satın alma, üretim vb.)</summary>
    StockIn = 1,

    /// <summary>Stok çıkışı (satış, iade vb.)</summary>
    StockOut = 2,

    /// <summary>Stok düzeltmesi (sayım sonrası)</summary>
    Adjustment = 3,

    /// <summary>Barkod tarama ile satış</summary>
    BarcodeSale = 4,

    /// <summary>Barkod tarama ile giriş</summary>
    BarcodeReceive = 5,

    /// <summary>OpenCart senkronizasyonu</summary>
    OpenCartSync = 6,

    /// <summary>Test compatibility - giriş</summary>
    In = 7,

    /// <summary>Test compatibility - çıkış</summary>
    Out = 8
}

// Movement Types Enum for reference
public static class MovementTypes
{
    public const string StockIn = "IN";
    public const string StockOut = "OUT";
    public const string Adjustment = "ADJUSTMENT";
    public const string Transfer = "TRANSFER";
    public const string Return = "RETURN";
    public const string Loss = "LOSS";
    public const string Found = "FOUND";
    public const string Sale = "SALE";
    public const string Purchase = "PURCHASE";
    public const string Production = "PRODUCTION";
    public const string Consumption = "CONSUMPTION";
}
