namespace MesTech.Application.DTOs.Finance;

/// <summary>
/// Expense data transfer object.
/// </summary>
public class ExpenseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
}
