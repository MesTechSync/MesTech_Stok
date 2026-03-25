using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Muhasebe modulu tedarikci hesabi — tedarikci bazinda bakiye ve son islem takibi.
/// Mevcut Domain.Entities.SupplierAccount'tan farkli: muhasebe odakli, basitleştirilmis.
/// </summary>
public sealed class AccountingSupplierAccount : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid SupplierId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public DateTime? LastTransactionDate { get; private set; }

    private AccountingSupplierAccount() { }

    public static AccountingSupplierAccount Create(
        Guid tenantId,
        Guid supplierId,
        string name,
        string currency = "TRY")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new AccountingSupplierAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SupplierId = supplierId,
            Name = name,
            Balance = 0m,
            Currency = currency,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AdjustBalance(decimal delta)
    {
        Balance += delta;
        LastTransactionDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
