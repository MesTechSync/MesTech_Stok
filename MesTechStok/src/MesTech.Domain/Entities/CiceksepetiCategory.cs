using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Ciceksepeti platform kategorisi.
/// Platform-specific — genel Category'den ayri tutulur.
/// </summary>
public class CiceksepetiCategory : BaseEntity
{
    public long CiceksepetiCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public long? ParentCategoryId { get; set; }
    public bool IsLeaf { get; set; }

    public override string ToString() => $"[CS-{CiceksepetiCategoryId}] {CategoryName}";
}
