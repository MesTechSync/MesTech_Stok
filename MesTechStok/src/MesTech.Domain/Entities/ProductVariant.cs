using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Ürün varyantı — renk/beden kombinasyonları.
/// </summary>
public class ProductVariant : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string? VariantSKU { get; set; }
    public string? VariantBarcode { get; set; }
    public int Stock { get; set; }
    public decimal? PriceOverride { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Product Product { get; set; } = null!;
    public ICollection<ProductPlatformMapping> PlatformMappings { get; set; } = new List<ProductPlatformMapping>();
}
