namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Profit Report data transfer object.
/// </summary>
public sealed class ProfitReportDto
{
    public Guid Id { get; set; }
    public DateTime ReportDate { get; set; }
    public string? Platform { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalCargo { get; set; }
    public decimal TotalTax { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public string Period { get; set; } = string.Empty;
}
