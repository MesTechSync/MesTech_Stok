namespace MesTech.Application.DTOs.Accounting;

public class FixedExpenseDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public int DayOfMonth { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? SupplierName { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
