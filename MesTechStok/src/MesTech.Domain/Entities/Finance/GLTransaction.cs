using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities.Finance;

public sealed class GLTransaction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? StoreId { get; private set; }
    public Guid? BankAccountId { get; private set; }
    public Guid? GLAccountId { get; private set; }
    public GLTransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal? ExchangeRate { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? ReferenceNumber { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public Guid? ExpenseId { get; private set; }
    public bool IsReconciled { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private GLTransaction() { }

    public static GLTransaction Create(
        Guid tenantId, GLTransactionType type, decimal amount, string description,
        Guid createdByUserId, string currency = "TRY",
        Guid? bankAccountId = null, Guid? glAccountId = null,
        Guid? orderId = null, Guid? invoiceId = null, Guid? expenseId = null,
        string? referenceNumber = null, Guid? storeId = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        return new GLTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StoreId = storeId,
            BankAccountId = bankAccountId,
            GLAccountId = glAccountId,
            Type = type,
            Amount = amount,
            Currency = currency,
            TransactionDate = DateTime.UtcNow,
            Description = description,
            ReferenceNumber = referenceNumber,
            OrderId = orderId,
            InvoiceId = invoiceId,
            ExpenseId = expenseId,
            CreatedByUserId = createdByUserId,
            IsReconciled = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Reconcile()
    {
        IsReconciled = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
