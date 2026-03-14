using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Dropshipping havuzundaki ürün kaydı.
/// Hangi ürünün hangi havuzda olduğunu, kim eklediğini, hangi fiyatla paylaşıldığını tutar.
/// </summary>
public class DropshippingPoolProduct : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Bağlı dropshipping havuzu.</summary>
    public Guid PoolId { get; private set; }

    /// <summary>Orijinal ürün.</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Havuz satış fiyatı (TRY). 0'dan büyük olmalı.</summary>
    public decimal PoolPrice { get; private set; }

    /// <summary>
    /// Bu ürünün hangi SupplierFeed'den havuza eklendiği.
    /// null: manuel eklendi.
    /// </summary>
    public Guid? AddedFromFeedId { get; private set; }

    /// <summary>Havuz ürünü aktif mi?</summary>
    public bool IsActive { get; private set; } = true;

    // Navigation
    public DropshippingPool? Pool { get; private set; }
    public Product? Product { get; private set; }

    // EF Core parametresiz ctor
    private DropshippingPoolProduct() { }

    public DropshippingPoolProduct(Guid tenantId, Guid poolId, Guid productId,
        decimal poolPrice, Guid? addedFromFeedId = null)
    {
        if (poolId == Guid.Empty)
            throw new ArgumentException("PoolId boş olamaz.", nameof(poolId));
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId boş olamaz.", nameof(productId));
        if (poolPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(poolPrice), "Havuz fiyatı negatif olamaz.");

        TenantId = tenantId;
        PoolId = poolId;
        ProductId = productId;
        PoolPrice = poolPrice;
        AddedFromFeedId = addedFromFeedId;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice), "Fiyat negatif olamaz.");

        PoolPrice = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }

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

    public override string ToString() =>
        $"PoolProduct [Pool:{PoolId}] [Product:{ProductId}] Price:{PoolPrice:F2} Active:{IsActive}";
}
