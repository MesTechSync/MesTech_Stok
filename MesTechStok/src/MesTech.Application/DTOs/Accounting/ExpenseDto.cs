using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Accounting;

public class ExpenseDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ExpenseType ExpenseType { get; set; }
    public DateTime Date { get; set; }
    public string? Note { get; set; }
}
