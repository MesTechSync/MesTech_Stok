namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Mutabakat eslestirmesi tamamlandiginda tetiklenir.
/// </summary>
public record ReconciliationMatchedEvent : AccountingDomainEvent
{
    public Guid ReconciliationMatchId { get; init; }
    public Guid? BankTransactionId { get; init; }
    public Guid? SettlementBatchId { get; init; }
    public decimal Confidence { get; init; }
}
