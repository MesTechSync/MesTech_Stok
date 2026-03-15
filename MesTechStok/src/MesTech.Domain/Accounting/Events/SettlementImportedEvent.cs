namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Platform hesap kesimi iceye aktarildiginda tetiklenir.
/// </summary>
public record SettlementImportedEvent : AccountingDomainEvent
{
    public Guid SettlementBatchId { get; init; }
    public string Platform { get; init; } = string.Empty;
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public decimal TotalNet { get; init; }
}
