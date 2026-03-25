using MesTech.Domain.Common;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities.Finance;

/// <summary>
/// Kasa entity — nakit giris/cikis takibi.
/// Her tenant'in birden fazla kasasi olabilir (TRY, USD, EUR).
/// </summary>
public sealed class CashRegister : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string CurrencyCode { get; private set; } = "TRY";
    public decimal Balance { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    private readonly List<CashTransaction> _transactions = new();
    public IReadOnlyCollection<CashTransaction> Transactions => _transactions.AsReadOnly();

    private CashRegister() { }

    public static CashRegister Create(
        Guid tenantId, string name, string currencyCode = "TRY",
        bool isDefault = false, decimal openingBalance = 0m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new CashRegister
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            CurrencyCode = currencyCode,
            Balance = openingBalance,
            IsDefault = isDefault,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Kasaya gelir kaydet — bakiye artar.</summary>
    public CashTransaction RecordIncome(decimal amount, string description, string? category = null)
    {
        if (amount <= 0) throw new InvalidOperationException("Gelir tutari pozitif olmali.");
        Balance += amount;
        UpdatedAt = DateTime.UtcNow;

        var tx = CashTransaction.Create(Id, TenantId, CashTransactionType.Income, amount, description, category);
        _transactions.Add(tx);
        RaiseDomainEvent(new CashTransactionRecordedEvent(TenantId, Id, tx.Id, CashTransactionType.Income, amount, Balance, DateTime.UtcNow));
        return tx;
    }

    /// <summary>Kasadan gider kaydet — bakiye azalir.</summary>
    public CashTransaction RecordExpense(decimal amount, string description, string? category = null)
    {
        if (amount <= 0) throw new InvalidOperationException("Gider tutari pozitif olmali.");
        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;

        var tx = CashTransaction.Create(Id, TenantId, CashTransactionType.Expense, amount, description, category);
        _transactions.Add(tx);
        RaiseDomainEvent(new CashTransactionRecordedEvent(TenantId, Id, tx.Id, CashTransactionType.Expense, amount, Balance, DateTime.UtcNow));
        return tx;
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

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
