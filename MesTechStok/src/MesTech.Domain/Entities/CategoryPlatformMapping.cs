using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Kategori-Platform eşleştirme — platformdaki kategori ID'si.
/// </summary>
public class CategoryPlatformMapping : BaseEntity
{
    public int CategoryId { get; set; }
    public int StoreId { get; set; }
    public PlatformType PlatformType { get; set; }
    public string? ExternalCategoryId { get; set; }
    public string? ExternalCategoryName { get; set; }
    public DateTime? LastSyncDate { get; set; }

    // Navigation
    public Category Category { get; set; } = null!;
    public Store Store { get; set; } = null!;
}
