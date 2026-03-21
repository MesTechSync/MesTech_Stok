namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Cash Flow Report data transfer object.
/// </summary>
public class CashFlowReportDto
{
    public decimal TotalInflow { get; set; }
    public decimal TotalOutflow { get; set; }
    public decimal NetFlow { get; set; }
    public List<CashFlowEntryDto> Entries { get; set; } = new();
}

public class CashFlowEntryDto
{
    public Guid Id { get; set; }
    public DateTime EntryDate { get; set; }
    public decimal Amount { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public Guid? CounterpartyId { get; set; }
    public string? CounterpartyName { get; set; }
}
