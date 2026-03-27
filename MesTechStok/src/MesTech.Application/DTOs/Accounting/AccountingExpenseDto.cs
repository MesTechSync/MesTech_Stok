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
    public string? SupplierName { get; set; }
    public string? Description { get; set; }
    public string? GlAccountCode { get; set; }
    public DateTime ExpenseDate { get; set; }
    public DateTime Date => ExpenseDate;
    public string Source { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; }
}
