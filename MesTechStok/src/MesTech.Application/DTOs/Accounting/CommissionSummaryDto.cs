namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Commission Summary data transfer object.
/// </summary>
public sealed class CommissionSummaryDto
{
    public decimal TotalCommission { get; set; }
    public decimal TotalServiceFee { get; set; }
    public List<PlatformCommissionDto> ByPlatform { get; set; } = new();
}

public sealed class PlatformCommissionDto
{
    public string Platform { get; set; } = string.Empty;
    public decimal TotalGross { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal AverageRate { get; set; }
    public int RecordCount { get; set; }
}
