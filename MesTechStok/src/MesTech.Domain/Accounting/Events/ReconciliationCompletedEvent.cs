using MesTech.Domain.Accounting.Enums;

namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Mutabakat eslestirmesi tamamlandiginda (onay/red) tetiklenir.
/// Hem otomatik hem manuel eslestirme sonuclarini kapsar.
/// </summary>
public record ReconciliationCompletedEvent : AccountingDomainEvent
{
    public Guid MatchId { get; init; }
    public Guid? SettlementBatchId { get; init; }
    public Guid? BankTransactionId { get; init; }
    public ReconciliationStatus FinalStatus { get; init; }
    public decimal Confidence { get; init; }
}
