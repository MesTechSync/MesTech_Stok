namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// MESA AI mutabakat eslestirme onerisi geldiginde consume edilir.
/// Exchange: mestech.mesa.ai.reconciliation.suggested.v1
/// </summary>
public record AiReconciliationSuggestedEvent(
    Guid? SettlementBatchId,
    Guid? BankTransactionId,
    decimal Confidence,
    string? Rationale,
    Guid TenantId,
    DateTime OccurredAt);
