using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Exceptions;

namespace MesTech.Domain.Entities;

/// <summary>
/// Ürün Aggregate Root — Stok yönetim sisteminin merkezi entity'si.
/// </summary>
public sealed class Product : BaseEntity, ITenantEntity
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
    public bool IsActive { get; internal set; } = true;
    public bool IsDiscontinued { get; set; }
    public bool IsSerialized { get; set; }
    public bool IsBatchTracked { get; set; }
    public bool IsPerishable { get; set; }

    // Tarihler
    public DateTime? ExpiryDate { get; set; }
    public DateTime? LastStockUpdate { get; private set; }

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
    public decimal? RecommendedPrice { get; private set; }
    public DateTime? LastAiPriceAt { get; private set; }
    public int? PredictedDemand7d { get; private set; }
    public int? DaysUntilStockout { get; private set; }
    public DateTime? LastAiStockAt { get; private set; }

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
        // Guard: stok çıkışında negatife düşmeyi engelle
        if (quantity < 0 && Stock + quantity < 0)
            throw new InsufficientStockException(SKU, Stock, Math.Abs(quantity));

        var previousStock = Stock;
        Stock += quantity;
        LastStockUpdate = DateTime.UtcNow;

        RaiseDomainEvent(new StockChangedEvent(
            Id, TenantId, SKU, previousStock, Stock, movementType, DateTime.UtcNow));

        if (IsLowStock() && previousStock > MinimumStock)
        {
            RaiseDomainEvent(new LowStockDetectedEvent(
                Id, TenantId, SKU, Stock, MinimumStock, DateTime.UtcNow));
        }

        // Stok sıfıra düştüyse — platform pasife alma zinciri tetikle
        if (Stock <= 0 && previousStock > 0)
        {
            RaiseDomainEvent(new ZeroStockDetectedEvent(
                Id, TenantId, SKU, previousStock, DateTime.UtcNow));
        }

        if (IsCriticalStock || IsOutOfStock())
        {
            var level = IsOutOfStock() ? StockAlertLevel.OutOfStock
                : IsCriticalStock ? StockAlertLevel.Critical
                : StockAlertLevel.Low;
            RaiseDomainEvent(new StockCriticalEvent(
                Id, TenantId, Name, SKU, Stock, MinimumStock, level, null, null, DateTime.UtcNow));
        }
    }

    public void UpdatePrice(decimal newSalePrice)
    {
        if (newSalePrice < 0)
            throw new ArgumentException("Satış fiyatı negatif olamaz.", nameof(newSalePrice));
        if (newSalePrice == SalePrice) return;
        var oldPrice = SalePrice;
        SalePrice = newSalePrice;

        RaiseDomainEvent(new PriceChangedEvent(
            Id, TenantId, SKU, oldPrice, newSalePrice, DateTime.UtcNow));

        // Zarar kontrolü — satış fiyatı alışın altına düştüyse uyarı
        if (PurchasePrice > 0 && newSalePrice < PurchasePrice)
        {
            var lossPerUnit = PurchasePrice - newSalePrice;
            RaiseDomainEvent(new PriceLossDetectedEvent(
                Id, TenantId, SKU, PurchasePrice, newSalePrice, lossPerUnit, DateTime.UtcNow));
        }
    }

    public void AddStock(int quantity, string? reference = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Stok miktarı pozitif olmalı.", nameof(quantity));
        AdjustStock(quantity, StockMovementType.StockIn, reference);
    }

    public void RemoveStock(int quantity, string? reference = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Stok miktarı pozitif olmalı.", nameof(quantity));
        if (Stock < quantity)
            throw new InsufficientStockException(SKU, Stock, quantity);
        AdjustStock(-quantity, StockMovementType.StockOut, reference);
    }

    public bool IsCriticalStock => Stock <= MinimumStock && Stock > 0;
    public bool IsLowStockRange => Stock <= (int)(MinimumStock * 1.5m) && !IsCriticalStock && !IsOutOfStock();

    public bool IsLowStock() => Stock <= MinimumStock;
    public bool IsOutOfStock() => Stock <= 0;
    public bool IsOverStock() => MaximumStock > 0 && Stock > MaximumStock;
    public bool NeedsReorder() => Stock <= ReorderLevel;

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal ProfitMargin => SalePrice > 0 && PurchasePrice > 0
        ? ((SalePrice - PurchasePrice) / SalePrice) * 100
        : 0;

    public decimal TotalValue => Stock * PurchasePrice;

    /// <summary>
    /// Ürün oluşturulduktan sonra ProductCreatedEvent fırlatır.
    /// Handler veya factory'den çağrılmalı.
    /// </summary>
    public void MarkAsCreated()
    {
        RaiseDomainEvent(new ProductCreatedEvent(Id, TenantId, SKU, Name, SalePrice, DateTime.UtcNow));
    }

    /// <summary>
    /// Ürün bilgileri güncellendiğinde ProductUpdatedEvent fırlatır.
    /// </summary>
    public void MarkAsUpdated()
    {
        RaiseDomainEvent(new ProductUpdatedEvent(Id, TenantId, SKU, DateTime.UtcNow));
    }

    /// <summary>
    /// Buybox kaybedildiğinde fırlatılır — rakip fiyat daha düşük.
    /// Platform sync job'ından çağrılır.
    /// </summary>
    public void ReportBuyboxLost(decimal competitorPrice, string competitorName)
    {
        RaiseDomainEvent(new BuyboxLostEvent(Id, TenantId, SKU, SalePrice, competitorPrice, competitorName, DateTime.UtcNow));
    }

    public void UpdateAiPriceSnapshot(decimal recommendedPrice)
    {
        RecommendedPrice = recommendedPrice;
        LastAiPriceAt = DateTime.UtcNow;
    }

    public void UpdateAiStockSnapshot(int predictedDemand7d, int daysUntilStockout)
    {
        PredictedDemand7d = predictedDemand7d;
        DaysUntilStockout = daysUntilStockout;
        LastAiStockAt = DateTime.UtcNow;
    }

    public decimal? Volume => Length.HasValue && Width.HasValue && Height.HasValue
        ? Length.Value * Width.Value * Height.Value
        : null;

    public override string ToString() => $"[{SKU}] {Name} (Stok: {Stock})";
}
