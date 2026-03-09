using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Marka-Platform eşleştirme — platformdaki marka ID'si.
/// </summary>
public class BrandPlatformMapping : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid BrandId { get; set; }
    public Guid StoreId { get; set; }
    public PlatformType PlatformType { get; set; }
    public string? ExternalBrandId { get; set; }
    public string? ExternalBrandName { get; set; }
    public DateTime? LastSyncDate { get; set; }

    // Navigation
    public Brand Brand { get; set; } = null!;
    public Store Store { get; set; } = null!;
}
