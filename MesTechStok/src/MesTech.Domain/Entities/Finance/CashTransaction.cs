using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Finance;

/// <summary>
/// Kasa hareketi — gelir, gider veya transfer kaydi.
/// </summary>
public sealed class CashTransaction : BaseEntity, ITenantEntity
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
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
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
