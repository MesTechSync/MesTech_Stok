using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Marka entity'si.
/// </summary>
public class Brand : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<BrandPlatformMapping> PlatformMappings { get; set; } = new List<BrandPlatformMapping>();
}
