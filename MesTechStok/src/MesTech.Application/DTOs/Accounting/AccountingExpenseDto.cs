namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Accounting Expense data transfer object.
/// </summary>
public sealed class AccountingExpenseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Category { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; }
}
