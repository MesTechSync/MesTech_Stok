using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Finance;

namespace MesTech.Domain.Entities.Finance;

/// <summary>
/// Harcama/Gider kaydı — Finance modülü için (onay akışlı).
/// OnMuhasebe modülündeki Expense'den farklıdır.
/// </summary>
public sealed class FinanceExpense : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? StoreId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public DateTime ExpenseDate { get; private set; }
    public ExpenseCategory Category { get; private set; }
    public ExpenseStatus Status { get; private set; }
    public Guid? SubmittedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? Notes { get; private set; }
    public Guid? DocumentId { get; private set; }   // belge eki — nullable

    private FinanceExpense() { }

    public static FinanceExpense Create(
        Guid tenantId, string title, decimal amount, ExpenseCategory category,
        DateTime expenseDate, Guid? submittedByUserId = null,
        string? notes = null, Guid? storeId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");

        return new FinanceExpense
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StoreId = storeId,
            Title = title,
            Amount = amount,
            Category = category,
            ExpenseDate = expenseDate,
            Status = ExpenseStatus.Draft,
            SubmittedByUserId = submittedByUserId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Submit()
    {
        if (Status != ExpenseStatus.Draft)
            throw new InvalidOperationException($"Cannot submit an expense in {Status} status.");
        Status = ExpenseStatus.Submitted;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ExpenseSubmittedEvent(Id, TenantId, DateTime.UtcNow));
    }

    public void Approve(Guid approverUserId)
    {
        if (Status != ExpenseStatus.Submitted)
            throw new InvalidOperationException("Only submitted expenses can be approved.");
        Status = ExpenseStatus.Approved;
        ApprovedByUserId = approverUserId;
        ApprovedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ExpenseApprovedEvent(Id, TenantId, approverUserId, DateTime.UtcNow));
    }

    public void Reject(string reason)
    {
        if (Status != ExpenseStatus.Submitted)
            throw new InvalidOperationException("Only submitted expenses can be rejected.");
        Status = ExpenseStatus.Rejected;
        Notes = string.IsNullOrWhiteSpace(reason) ? Notes : $"Red: {reason}";
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ExpenseRejectedEvent(Id, TenantId, reason, DateTime.UtcNow));
    }

    public void MarkAsPaid(Guid bankAccountId)
    {
        if (Status != ExpenseStatus.Approved)
            throw new InvalidOperationException("Only approved expenses can be marked as paid.");
        Status = ExpenseStatus.Paid;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ExpensePaidEvent(Id, TenantId, bankAccountId, DateTime.UtcNow));
    }

    public void AttachDocument(Guid documentId)
    {
        DocumentId = documentId;
        UpdatedAt = DateTime.UtcNow;
    }
}
