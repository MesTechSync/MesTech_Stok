namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Kar/zarar raporu olusturuldugunda tetiklenir.
/// </summary>
public record ProfitReportGeneratedEvent : AccountingDomainEvent
{
    public Guid ReportId { get; init; }
    public string Period { get; init; } = string.Empty;
    public string? Platform { get; init; }
    public decimal NetProfit { get; init; }
}
