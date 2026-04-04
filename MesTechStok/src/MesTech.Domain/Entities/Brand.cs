using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Marka entity'si.
/// </summary>
public sealed class Brand : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<BrandPlatformMapping> PlatformMappings { get; set; } = new List<BrandPlatformMapping>();

    /// <summary>EF Core / test constructor.</summary>
    internal Brand() { }

    public static Brand Create(Guid tenantId, string name, string? logoUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        return new Brand
        {
            TenantId = tenantId,
            Name = name.Trim(),
            LogoUrl = logoUrl,
            IsActive = true
        };
    }

    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        Name = newName.Trim();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
