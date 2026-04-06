using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Urun teknik ozelligi — "Ekran Boyutu: 6.7 inç", "Renk: Siyah" gibi key-value.
/// Platform attribute'lari ile eslestirme PlatformSpecificData'da yapilir.
/// </summary>
public sealed class ProductSpecification : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; private set; }
    public string SpecGroup { get; private set; } = string.Empty;
    public string SpecName { get; private set; } = string.Empty;
    public string SpecValue { get; private set; } = string.Empty;
    public string? Unit { get; private set; }
    public int DisplayOrder { get; private set; }

    // Navigation
    public Product? Product { get; set; }

    private ProductSpecification() { }

    public static ProductSpecification Create(
        Guid tenantId, Guid productId, string group, string name, string value,
        string? unit = null, int displayOrder = 0)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId bos olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ProductSpecification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            SpecGroup = group ?? string.Empty,
            SpecName = name,
            SpecValue = value ?? string.Empty,
            Unit = unit,
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateValue(string newValue, string? newUnit = null)
    {
        SpecValue = newValue ?? string.Empty;
        if (newUnit is not null) Unit = newUnit;
        UpdatedAt = DateTime.UtcNow;
    }
}
