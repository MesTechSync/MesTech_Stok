using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Ürün-Platform eşleştirme — platformdaki ürün ID'si.
/// OpenCartProductId gibi alanlar buraya taşınır.
/// </summary>
public sealed class ProductPlatformMapping : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public Guid StoreId { get; set; }
    public PlatformType PlatformType { get; set; }
    public string? ExternalProductId { get; set; }
    public string? ExternalCategoryId { get; set; }
    public string? ExternalUrl { get; set; }

    // D12-03: Platform-specific identifiers (index'lenebilir — PlatformSpecificData JSON'dan ayri)
    public string? PlatformBarcode { get; set; }
    public string? PlatformModelCode { get; set; }
    public string? PlatformStockCode { get; set; }

    public SyncStatus SyncStatus { get; set; } = SyncStatus.NotSynced;
    public DateTime? LastSyncDate { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Platform'a ozel metadata — JSON serialized.
    /// Ciceksepeti: DeliveryType, StockCode vb.
    /// Hepsiburada: ListingStatus, CommissionRate vb.
    /// </summary>
    public string? PlatformSpecificData { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
    public ProductVariant? ProductVariant { get; set; }
    public Store Store { get; set; } = null!;
}
