using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Expense data transfer object.
/// </summary>
public sealed class ExpenseDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ExpenseType ExpenseType { get; set; }
    public DateTime Date { get; set; }
    public string? Note { get; set; }
}
