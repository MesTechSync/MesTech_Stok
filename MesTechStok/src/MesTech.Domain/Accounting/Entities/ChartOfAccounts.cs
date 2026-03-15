using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.ValueObjects;
using MesTech.Domain.Common;
using AccountType = MesTech.Domain.Accounting.Enums.AccountType;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Hesap Plani — Tekduzen Hesap Plani (THP) destekli.
/// Hiyerarsik yapida: ParentId ile alt hesaplar tanimlanir.
/// </summary>
public class ChartOfAccounts : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public AccountType AccountType { get; private set; }
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; }
    public int Level { get; private set; }

    // Navigation
    public ChartOfAccounts? Parent { get; private set; }

    private ChartOfAccounts() { }

    public static ChartOfAccounts Create(
        Guid tenantId,
        string code,
        string name,
        AccountType accountType,
        Guid? parentId = null,
        int level = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ChartOfAccounts
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = name,
            AccountType = accountType,
            ParentId = parentId,
            IsActive = true,
            Level = level,
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
