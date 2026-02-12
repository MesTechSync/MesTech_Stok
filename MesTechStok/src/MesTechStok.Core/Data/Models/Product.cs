using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models;

/// <summary>
/// Ürün bilgilerini tutan ana model sınıfı
/// Barkodlu stok takip sisteminin temel varlığıdır
/// </summary>
public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Barcode { get; set; } = string.Empty;

    // GS1 Standards
    [MaxLength(14)]
    public string? GTIN { get; set; }

    [MaxLength(20)]
    public string? UPC { get; set; }

    [MaxLength(20)]
    public string? EAN { get; set; }

    // Pricing
    [Column(TypeName = "decimal(18,2)")]
    public decimal PurchasePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ListPrice { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxRate { get; set; } = 0.18m; // KDV %18

    // Discount (optional, UI inline edit)
    [Column(TypeName = "decimal(5,2)")]
    public decimal? DiscountRate { get; set; }

    // Stock Information
    public int Stock { get; set; }
    public int MinimumStock { get; set; } = 5;
    public int MaximumStock { get; set; } = 1000;
    public int ReorderLevel { get; set; } = 10;
    public int ReorderQuantity { get; set; } = 50;

    // Category & Classification
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;

    public int? SupplierId { get; set; }
    public virtual Supplier? Supplier { get; set; }

    // Physical Properties
    [Column(TypeName = "decimal(10,3)")]
    public decimal? Weight { get; set; } // kg

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Length { get; set; } // cm

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Width { get; set; } // cm

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Height { get; set; } // cm

    [MaxLength(10)]
    public string? WeightUnit { get; set; } = "kg";

    [MaxLength(10)]
    public string? DimensionUnit { get; set; } = "cm";

    // Location & Organization
    [MaxLength(50)]
    public string? Location { get; set; }

    [MaxLength(20)]
    public string? Shelf { get; set; }

    [MaxLength(20)]
    public string? Bin { get; set; }

    public int? WarehouseId { get; set; }
    public virtual Warehouse? Warehouse { get; set; }

    // Status & Flags
    public bool IsActive { get; set; } = true;
    public bool IsDiscontinued { get; set; } = false;
    public bool IsSerialized { get; set; } = false;
    public bool IsBatchTracked { get; set; } = false;
    public bool IsPerishable { get; set; } = false;

    // Dates
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? ModifiedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? LastStockUpdate { get; set; }

    // User Tracking
    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    [MaxLength(50)]
    public string? ModifiedBy { get; set; }

    // Images & Documents
    [MaxLength(255)]
    public string? ImageUrl { get; set; }

    [MaxLength(500)]
    public string? ImageUrls { get; set; } // JSON array of image URLs

    [MaxLength(500)]
    public string? DocumentUrls { get; set; } // JSON array of document URLs

    // Additional Properties
    [MaxLength(50)]
    public string? Brand { get; set; }

    [MaxLength(50)]
    public string? Model { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    [MaxLength(20)]
    public string? Size { get; set; }

    // Apparel-style multi-size list
    [MaxLength(50)]
    public string? Sizes { get; set; } // Comma-separated e.g., "2S,S,M,L,XL,2XL,3XL"

    // Commerce attributes (popup uyumu)
    [MaxLength(50)]
    public string? Origin { get; set; }

    [MaxLength(50)]
    public string? Material { get; set; }

    [MaxLength(50)]
    public string? VolumeText { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Desi { get; set; }

    public int? LeadTimeDays { get; set; }

    [MaxLength(255)]
    public string? ShipAddress { get; set; }

    [MaxLength(255)]
    public string? ReturnAddress { get; set; }

    // Audit/Regulatory information
    [MaxLength(1000)]
    public string? UsageInstructions { get; set; }

    [MaxLength(255)]
    public string? ImporterInfo { get; set; }

    [MaxLength(255)]
    public string? ManufacturerInfo { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? Tags { get; set; } // Comma-separated tags

    // OpenCart Integration
    [MaxLength(50)]
    public string? Code { get; set; } // Ürün kodu

    public int? OpenCartProductId { get; set; }
    public int? OpenCartCategoryId { get; set; }
    public int? ParentCategoryId { get; set; }

    public bool ShowInMenu { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    public DateTime? LastSyncDate { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public bool SyncWithOpenCart { get; set; } = true;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Additional properties needed by OpenCart sync and testing
    [NotMapped]
    public string Sku { get => SKU; set => SKU = value; } // Alias for SKU

    [NotMapped]
    public decimal Price { get => SalePrice; set => SalePrice = value; } // Alias for SalePrice

    [NotMapped]
    public int StockQuantity { get => Stock; set => Stock = value; } // Alias for Stock

    [NotMapped]
    public DateTime CreatedAt { get => CreatedDate; set => CreatedDate = value; } // Alias for CreatedDate

    // ALPHA TEAM: Added UpdatedAt property for stock service compatibility
    [NotMapped]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Calculated Properties
    [NotMapped]
    public decimal ProfitMargin => SalePrice > 0 ? ((SalePrice - PurchasePrice) / SalePrice) * 100 : 0;

    [NotMapped]
    public decimal TotalValue => Stock * PurchasePrice;

    // Costing: Ağırlıklı ortalama birim maliyet (varsayılan PurchasePrice kullanılır, ileride farklılaşabilir)
    [NotMapped]
    public decimal WeightedAverageUnitCost => PurchasePrice;

    [NotMapped]
    public string StockStatus
    {
        get
        {
            if (Stock == 0) return "Tükendi";
            if (Stock <= MinimumStock) return "Düşük Stok";
            if (Stock <= ReorderLevel) return "Yeniden Sipariş";
            return "Yeterli";
        }
    }

    [NotMapped]
    public string StockStatusColor
    {
        get
        {
            return StockStatus switch
            {
                "Tükendi" => "#F44336",
                "Düşük Stok" => "#FF9800",
                "Yeniden Sipariş" => "#2196F3",
                _ => "#4CAF50"
            };
        }
    }

    [NotMapped]
    public bool NeedsReorder => Stock <= ReorderLevel && IsActive && !IsDiscontinued;

    [NotMapped]
    public decimal Volume => (Length ?? 0) * (Width ?? 0) * (Height ?? 0);

    // Stok Yerleşim Sistemi Computed Properties - TEMPORARILY DISABLED
    /*
    [NotMapped]
    public string FullLocationPath
    {
        get
        {
            var primaryLocation = ProductLocations?.FirstOrDefault(pl => pl.IsPrimary);
            if (primaryLocation == null) return "Konum Belirtilmemiş";
            
            return $"{primaryLocation.Bin?.Shelf?.Rack?.Zone?.Name} → " +
                   $"{primaryLocation.Bin?.Shelf?.Rack?.Name} → " +
                   $"{primaryLocation.Bin?.Shelf?.Name} → " +
                   $"{primaryLocation.Bin?.Name}";
        }
    }
    
    [NotMapped]
    public string QuickLocationCode
    {
        get
        {
            var primaryLocation = ProductLocations?.FirstOrDefault(pl => pl.IsPrimary);
            return primaryLocation?.Bin?.Code ?? "N/A";
        }
    }
    */

    // Navigation Properties for EF Core
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<InventoryLot> InventoryLots { get; set; } = new List<InventoryLot>();
    // TEMPORARILY DISABLED: public virtual ICollection<ProductLocation> ProductLocations { get; set; } = new List<ProductLocation>();

    // Methods
    public void UpdateStock(int quantity, string movementType, string? reason = null)
    {
        Stock += quantity;
        LastStockUpdate = DateTime.Now;

        // Create stock movement record
        var movement = new StockMovement
        {
            ProductId = Id,
            Quantity = quantity,
            MovementType = movementType,
            Reason = reason,
            Date = DateTime.Now,
            NewStockLevel = Stock
        };

        StockMovements.Add(movement);
    }

    public bool IsLowStock() => Stock <= MinimumStock;
    public bool IsOutOfStock() => Stock <= 0;
    public bool IsOverStock() => Stock >= MaximumStock;

    public override string ToString() => $"{Name} (SKU: {SKU})";
}
