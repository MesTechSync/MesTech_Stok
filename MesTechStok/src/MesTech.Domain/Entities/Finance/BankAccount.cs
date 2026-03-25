using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Finance;

public sealed class BankAccount : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? StoreId { get; private set; }
    public string AccountName { get; private set; } = string.Empty;
    public string? BankName { get; private set; }
    public string? IBAN { get; private set; }
    public string? AccountNumber { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal Balance { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDefault { get; private set; }

    private BankAccount() { }

    public static BankAccount Create(
        Guid tenantId, string accountName, string currency = "TRY",
        string? bankName = null, string? iban = null,
        string? accountNumber = null, bool isDefault = false,
        Guid? storeId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        return new BankAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StoreId = storeId,
            AccountName = accountName,
            BankName = bankName,
            IBAN = iban,
            AccountNumber = accountNumber,
            Currency = currency,
            Balance = 0m,
            IsActive = true,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AdjustBalance(decimal delta)
    {
        Balance += delta;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
