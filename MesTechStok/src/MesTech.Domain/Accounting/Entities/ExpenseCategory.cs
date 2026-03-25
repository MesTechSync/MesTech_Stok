using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Gider kategorisi — hiyerarsik (ParentId ile alt kategori destegi).
/// </summary>
public sealed class ExpenseCategory : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation
    public ExpenseCategory? Parent { get; private set; }

    private ExpenseCategory() { }

    public static ExpenseCategory Create(
        Guid tenantId,
        string name,
        string? code = null,
        Guid? parentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ExpenseCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Code = code,
            ParentId = parentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }
}
