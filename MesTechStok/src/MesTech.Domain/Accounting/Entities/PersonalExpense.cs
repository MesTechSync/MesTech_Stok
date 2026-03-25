using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Kisisel/operasyonel gider kaydi — onay akisli.
/// </summary>
public sealed class PersonalExpense : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Title { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string? Category { get; private set; }
    public DateTime ExpenseDate { get; private set; }
    public ExpenseSource Source { get; private set; }
    public bool IsApproved { get; private set; }
    public string? ApprovedBy { get; private set; }

    private PersonalExpense() { }

    public static PersonalExpense Create(
        Guid tenantId,
        string title,
        decimal amount,
        DateTime expenseDate,
        ExpenseSource source,
        string? category = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");

        var expense = new PersonalExpense
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            Amount = amount,
            Category = category,
            ExpenseDate = expenseDate,
            Source = source,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };

        expense.RaiseDomainEvent(new ExpenseCreatedEvent
        {
            TenantId = tenantId,
            ExpenseId = expense.Id,
            Title = title,
            Amount = amount,
            Source = source
        });

        return expense;
    }

    public void Approve(string approvedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);
        IsApproved = true;
        ApprovedBy = approvedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
