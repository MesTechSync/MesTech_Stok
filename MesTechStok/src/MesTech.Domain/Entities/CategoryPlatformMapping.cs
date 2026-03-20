using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Kategori-Platform eşleştirme — platformdaki kategori ID'si.
/// AI auto-mapping desteği, güven skoru ve kategori yolu takibi.
/// </summary>
public class CategoryPlatformMapping : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid StoreId { get; set; }
    public PlatformType PlatformType { get; set; }
    public string? ExternalCategoryId { get; set; }
    public string? ExternalCategoryName { get; set; }
    public DateTime? LastSyncDate { get; set; }

    // Enhanced mapping metadata
    public bool IsAutoMapped { get; set; } = false;
    public decimal? MatchConfidence { get; set; }
    public DateTime MappedAt { get; set; } = DateTime.UtcNow;
    public string? MappedBy { get; set; }
    public string? InternalCategoryPath { get; set; }
    public string? PlatformCategoryPath { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    // Navigation
    public Category Category { get; set; } = null!;
    public Store Store { get; set; } = null!;

    // ── Domain Factory Method ──

    public static CategoryPlatformMapping Create(
        Guid categoryId,
        Guid storeId,
        PlatformType platformType,
        string externalCategoryId,
        string externalCategoryName,
        bool isAutoMapped = false,
        decimal? matchConfidence = null)
    {
        return new CategoryPlatformMapping
        {
            CategoryId = categoryId,
            StoreId = storeId,
            PlatformType = platformType,
            ExternalCategoryId = externalCategoryId,
            ExternalCategoryName = externalCategoryName,
            IsAutoMapped = isAutoMapped,
            MatchConfidence = matchConfidence,
            MappedAt = DateTime.UtcNow,
            LastSyncDate = DateTime.UtcNow,
            IsActive = true
        };
    }
}
