using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Ürün-Platform eşleştirme — platformdaki ürün ID'si.
/// OpenCartProductId gibi alanlar buraya taşınır.
/// </summary>
public class ProductPlatformMapping : BaseEntity
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int StoreId { get; set; }
    public PlatformType PlatformType { get; set; }
    public string? ExternalProductId { get; set; }
    public string? ExternalCategoryId { get; set; }
    public string? ExternalUrl { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.NotSynced;
    public DateTime? LastSyncDate { get; set; }
    public bool IsEnabled { get; set; } = true;

    // Navigation
    public Product Product { get; set; } = null!;
    public ProductVariant? ProductVariant { get; set; }
    public Store Store { get; set; } = null!;
}
