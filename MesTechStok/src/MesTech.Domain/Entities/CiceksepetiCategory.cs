using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Ciceksepeti platform kategori entity'si.
/// Ciceksepeti'nin kendi kategori agaci — leaf kategorilere urun eslenir.
/// </summary>
public class CiceksepetiCategory : BaseEntity
{
    public long CiceksepetiCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public long? ParentCategoryId { get; set; }
    public bool IsLeaf { get; set; }
}
