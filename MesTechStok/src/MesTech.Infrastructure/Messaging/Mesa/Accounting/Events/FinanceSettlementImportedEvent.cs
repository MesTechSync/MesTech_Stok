namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// Platform hesap kesimi iceye aktarildiginda publish edilir.
/// Exchange: mestech.mesa.finance.settlement.imported.v1
/// </summary>
public record FinanceSettlementImportedEvent(
    Guid SettlementBatchId,
    string Platform,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal TotalNet,
    int LineCount,
    Guid TenantId,
    DateTime OccurredAt);
