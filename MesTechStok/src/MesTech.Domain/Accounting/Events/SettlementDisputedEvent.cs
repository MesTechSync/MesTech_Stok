namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Platform hesap kesimi uyusmazlik durumuna gectiginde tetiklenir.
/// </summary>
public record SettlementDisputedEvent : AccountingDomainEvent
{
    public Guid SettlementBatchId { get; init; }
    public string Platform { get; init; } = string.Empty;
    public decimal TotalNet { get; init; }
}
