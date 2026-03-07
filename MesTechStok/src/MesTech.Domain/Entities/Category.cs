using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Ürün kategorisi.
/// </summary>
public class Category : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowInMenu { get; set; } = true;

    // Navigation
    private readonly List<Product> _products = new();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    private readonly List<Category> _subCategories = new();
    public IReadOnlyCollection<Category> SubCategories => _subCategories.AsReadOnly();

    public override string ToString() => $"[{Code}] {Name}";
}
