using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities;

/// <summary>
/// Ürün Aggregate Root — Stok yönetim sisteminin merkezi entity'si.
/// </summary>
public class Product : BaseEntity, ITenantEntity
{
    // Multi-tenant
    public Guid TenantId { get; set; }

    // Temel bilgiler
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }

    // Fiyatlandırma
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal? ListPrice { get; set; }
    public decimal TaxRate { get; set; } = 0.18m;
    public decimal? DiscountRate { get; set; }
    public decimal? DiscountedPrice { get; set; }

    // Stok
    public int Stock { get; set; }
    public int MinimumStock { get; set; } = 5;
    public int MaximumStock { get; set; } = 1000;
    public int ReorderLevel { get; set; } = 10;
    public int ReorderQuantity { get; set; } = 50;

    // İlişkiler (FK)
    public Guid CategoryId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? WarehouseId { get; set; }

    // Fiziksel özellikler
    public decimal? Weight { get; set; }
    public WeightUnit? WeightUnit { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? DimensionUnit { get; set; }
    public decimal? Desi { get; set; }

    // Lokasyon
    public string? Location { get; set; }
    public string? Shelf { get; set; }
    public string? Bin { get; set; }

    // Durum bayrakları
    public bool IsActive { get; set; } = true;
    public bool IsDiscontinued { get; set; }
    public bool IsSerialized { get; set; }
    public bool IsBatchTracked { get; set; }
    public bool IsPerishable { get; set; }

    // Tarihler
    public DateTime? ExpiryDate { get; set; }
    public DateTime? LastStockUpdate { get; set; }

    // Marka/Model/Varyant
    public Guid? BrandId { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public bool HasVariants { get; set; } = false;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string? Sizes { get; set; }
    public string? Origin { get; set; }
    public string? Material { get; set; }
    public string? VolumeText { get; set; }

    // Lojistik
    public int? LeadTimeDays { get; set; }
    public string? ShipAddress { get; set; }
    public string? ReturnAddress { get; set; }

    // Regülasyon
    public string? UsageInstructions { get; set; }
    public string? ImporterInfo { get; set; }
    public string? ManufacturerInfo { get; set; }

    // Görsel
    public string? ImageUrl { get; set; }

    // Notlar/Etiketler
    public string? Notes { get; set; }
    public string? Tags { get; set; }
    public string? Code { get; set; }

    // AI Snapshot (JOIN'siz dashboard gosterim)
    public decimal? RecommendedPrice { get; set; }
    public DateTime? LastAiPriceAt { get; set; }
    public int? PredictedDemand7d { get; set; }
    public int? DaysUntilStockout { get; set; }
    public DateTime? LastAiStockAt { get; set; }

    // Concurrency
    public byte[]? RowVersion { get; set; }

    // Navigation
    private readonly List<StockMovement> _stockMovements = new();
    public IReadOnlyCollection<StockMovement> StockMovements => _stockMovements.AsReadOnly();

    private readonly List<OrderItem> _orderItems = new();
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    private readonly List<InventoryLot> _inventoryLots = new();
    public IReadOnlyCollection<InventoryLot> InventoryLots => _inventoryLots.AsReadOnly();

    // Platform Mapping & Variant navigation
    public Brand? BrandEntity { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductPlatformMapping> PlatformMappings { get; set; } = new List<ProductPlatformMapping>();

    // ── Domain Business Logic ──

    public void AdjustStock(int quantity, StockMovementType movementType, string? reason = null)
    {
        var previousStock = Stock;
        Stock += quantity;
        LastStockUpdate = DateTime.UtcNow;

        RaiseDomainEvent(new StockChangedEvent(
            Id, SKU, previousStock, Stock, movementType, DateTime.UtcNow));

        if (IsLowStock() && previousStock > MinimumStock)
        {
            RaiseDomainEvent(new LowStockDetectedEvent(
                Id, SKU, Stock, MinimumStock, DateTime.UtcNow));
        }
    }

    public void UpdatePrice(decimal newSalePrice)
    {
        if (newSalePrice == SalePrice) return;
        var oldPrice = SalePrice;
        SalePrice = newSalePrice;

        RaiseDomainEvent(new PriceChangedEvent(
            Id, SKU, oldPrice, newSalePrice, DateTime.UtcNow));
    }

    public bool IsLowStock() => Stock <= MinimumStock;
    public bool IsOutOfStock() => Stock <= 0;
    public bool IsOverStock() => MaximumStock > 0 && Stock > MaximumStock;
    public bool NeedsReorder() => Stock <= ReorderLevel;

    public decimal ProfitMargin => SalePrice > 0 && PurchasePrice > 0
        ? ((SalePrice - PurchasePrice) / SalePrice) * 100
        : 0;

    public decimal TotalValue => Stock * PurchasePrice;

    public decimal? Volume => Length.HasValue && Width.HasValue && Height.HasValue
        ? Length.Value * Width.Value * Height.Value
        : null;

    public override string ToString() => $"[{SKU}] {Name} (Stok: {Stock})";
}
