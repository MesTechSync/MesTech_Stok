namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// Mutabakat eslestirme inceleme gerektirdiginde publish edilir.
/// Exchange: mestech.mesa.finance.reconciliation.pending.v1
/// </summary>
public record FinanceReconciliationPendingEvent(
    Guid MatchId,
    Guid? SettlementBatchId,
    Guid? BankTransactionId,
    decimal Confidence,
    decimal SettlementAmount,
    decimal BankTxAmount,
    Guid TenantId,
    DateTime OccurredAt);
