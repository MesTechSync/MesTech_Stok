using MesTech.Domain.Common;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities;

/// <summary>
/// Ürün kategorisi.
/// </summary>
public sealed class Category : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public string? InternalCategoryPath { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowInMenu { get; set; } = true;

    // Navigation
    private readonly List<Product> _products = new();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    private readonly List<Category> _subCategories = new();
    public IReadOnlyCollection<Category> SubCategories => _subCategories.AsReadOnly();

    // ── Factory ──

    public static Category Create(Guid tenantId, string name, string code, bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Code = code,
            IsActive = isActive
        };

        category.RaiseDomainEvent(new CategoryCreatedEvent(
            category.Id, tenantId, name, code, DateTime.UtcNow));

        return category;
    }

    public override string ToString() => $"[{Code}] {Name}";
}
