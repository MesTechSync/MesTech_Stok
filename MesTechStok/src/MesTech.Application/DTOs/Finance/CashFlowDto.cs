namespace MesTech.Application.DTOs.Finance;

/// <summary>
/// Cash Flow data transfer object.
/// </summary>
public sealed class CashFlowDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalInflows { get; set; }
    public decimal TotalOutflows { get; set; }
    public decimal NetCashFlow => TotalInflows - TotalOutflows;
    public List<CashFlowItemDto> Items { get; set; } = new();
}

public sealed class CashFlowItemDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsInflow { get; set; }
}
