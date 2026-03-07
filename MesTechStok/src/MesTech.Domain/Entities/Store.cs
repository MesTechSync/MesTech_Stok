using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Platform mağazası — bir tenant'ın bir e-ticaret platformundaki mağazası.
/// </summary>
public class Store : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }
    public PlatformType PlatformType { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? ExternalStoreId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<StoreCredential> Credentials { get; set; } = new List<StoreCredential>();
    public ICollection<ProductPlatformMapping> ProductMappings { get; set; } = new List<ProductPlatformMapping>();
}
