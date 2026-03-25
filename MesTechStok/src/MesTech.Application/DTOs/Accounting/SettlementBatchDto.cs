namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Settlement Batch data transfer object.
/// </summary>
public sealed class SettlementBatchDto
{
    public Guid Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalNet { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }
    public int LineCount { get; set; }
}
