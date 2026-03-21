using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Finance;

/// <summary>
/// Kasa hareketi — gelir, gider veya transfer kaydi.
/// </summary>
public class CashTransaction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CashRegisterId { get; private set; }
    public CashTransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? Category { get; private set; }
    public Guid? RelatedInvoiceId { get; private set; }
    public Guid? RelatedCurrentAccountId { get; private set; }
    public DateTime TransactionDate { get; private set; }

    // Navigation
    public CashRegister? CashRegister { get; private set; }

    private CashTransaction() { }

    public static CashTransaction Create(
        Guid cashRegisterId, Guid tenantId,
        CashTransactionType type, decimal amount,
        string description, string? category = null,
        Guid? relatedInvoiceId = null,
        Guid? relatedCurrentAccountId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (amount <= 0) throw new ArgumentException("Tutar pozitif olmali.", nameof(amount));

        return new CashTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CashRegisterId = cashRegisterId,
            Type = type,
            Amount = amount,
            Description = description,
            Category = category,
            RelatedInvoiceId = relatedInvoiceId,
            RelatedCurrentAccountId = relatedCurrentAccountId,
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>Kasa hareket turleri.</summary>
public enum CashTransactionType
{
    Income = 0,
    Expense = 1,
    Transfer = 2
}
