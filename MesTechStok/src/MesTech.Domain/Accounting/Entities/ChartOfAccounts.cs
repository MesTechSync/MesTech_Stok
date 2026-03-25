using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.ValueObjects;
using MesTech.Domain.Common;
using AccountType = MesTech.Domain.Accounting.Enums.AccountType;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Hesap Plani — Tekduzen Hesap Plani (THP) destekli.
/// Hiyerarsik yapida: ParentId ile alt hesaplar tanimlanir.
/// </summary>
public sealed class ChartOfAccounts : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public AccountType AccountType { get; private set; }
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsSystem { get; private set; }
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
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));

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
            IsSystem = false,
            Level = level,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Guards against modification of system accounts.
    /// Throws InvalidOperationException if this is a system account.
    /// </summary>
    public void EnsureNotSystem()
    {
        if (IsSystem)
            throw new InvalidOperationException(
                $"System account '{Code} - {Name}' cannot be modified or deleted.");
    }

    public void Deactivate()
    {
        EnsureNotSystem();
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
        EnsureNotSystem();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this account as a system account. Only for seed data.
    /// </summary>
    public void MarkAsSystem()
    {
        IsSystem = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this account for deletion. System accounts cannot be deleted.
    /// </summary>
    public void MarkDeleted(string deletedBy)
    {
        EnsureNotSystem();
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
