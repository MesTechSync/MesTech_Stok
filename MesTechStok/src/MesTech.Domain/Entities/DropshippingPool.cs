using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Dropshipping ürün havuzu.
/// Birden fazla tedarikçiden gelen ürünlerin tek havuzda toplandığı reseller senaryosu için kullanılır.
/// IsPublic=true ise diğer tenant'lar da havuzu görebilir (çok kiracılı dropshipping marketplace).
/// </summary>
public class DropshippingPool : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Havuz adı. Zorunlu, max 200 karakter.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Havuz açıklaması. Opsiyonel, max 1000 karakter.</summary>
    public string? Description { get; private set; }

    /// <summary>Havuz aktif mi?</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Havuz herkese açık mı?
    /// true: diğer tenant'lar da ürünleri görebilir/sipariş verebilir.
    /// false: yalnızca sahibi (TenantId) görebilir.
    /// </summary>
    public bool IsPublic { get; private set; } = false;

    /// <summary>Fiyat belirleme stratejisi.</summary>
    public PoolPricingStrategy PricingStrategy { get; private set; } = PoolPricingStrategy.Markup;

    // Navigation
    private readonly List<DropshippingPoolProduct> _products = new();
    public IReadOnlyCollection<DropshippingPoolProduct> Products => _products.AsReadOnly();

    // EF Core parametresiz ctor
    private DropshippingPool() { }

    public DropshippingPool(Guid tenantId, string name, string? description = null,
        bool isPublic = false, PoolPricingStrategy pricingStrategy = PoolPricingStrategy.Markup)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Havuz adı boş olamaz.", nameof(name));

        TenantId = tenantId;
        Name = name.Trim();
        Description = description?.Trim();
        IsPublic = isPublic;
        PricingStrategy = pricingStrategy;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void Update(string name, string? description, bool isPublic, PoolPricingStrategy pricingStrategy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Havuz adı boş olamaz.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        IsPublic = isPublic;
        PricingStrategy = pricingStrategy;
        UpdatedAt = DateTime.UtcNow;
    }

    public override string ToString() => $"Pool [{Name}] ({PricingStrategy}) — IsPublic:{IsPublic}";
}
