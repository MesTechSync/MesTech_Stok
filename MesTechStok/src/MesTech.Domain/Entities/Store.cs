using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Platform mağazası — bir tenant'ın bir e-ticaret platformundaki mağazası.
/// </summary>
public sealed class Store : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public PlatformType PlatformType { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? ExternalStoreId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoSyncInvoice { get; set; }

    // ── Muhasebe Modulu (MUH-01) ──
    public decimal? CurrentAccountBalance { get; set; }
    public DateTime? LastSettlementDate { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<StoreCredential> Credentials { get; set; } = new List<StoreCredential>();
    public ICollection<ProductPlatformMapping> ProductMappings { get; set; } = new List<ProductPlatformMapping>();
}
