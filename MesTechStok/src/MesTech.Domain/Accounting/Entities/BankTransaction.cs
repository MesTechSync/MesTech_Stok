using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Banka hareketi — ekstre satirlari, idempotency key ile tekrar engeli.
/// Mevcut Finance.BankAccount entity'sine referans verir.
/// </summary>
public sealed class BankTransaction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid BankAccountId { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? ReferenceNumber { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public bool IsReconciled { get; private set; }

    private BankTransaction() { }

    public static BankTransaction Create(
        Guid tenantId,
        Guid bankAccountId,
        DateTime transactionDate,
        decimal amount,
        string description,
        string? referenceNumber = null,
        string? idempotencyKey = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        if (bankAccountId == Guid.Empty)
            throw new ArgumentException("BankAccountId boş olamaz.", nameof(bankAccountId));
        if (amount == 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Tutar sıfır olamaz.");
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new BankTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BankAccountId = bankAccountId,
            TransactionDate = transactionDate,
            Amount = amount,
            Description = description,
            ReferenceNumber = referenceNumber,
            IdempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString("N"),
            IsReconciled = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkReconciled()
    {
        IsReconciled = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
