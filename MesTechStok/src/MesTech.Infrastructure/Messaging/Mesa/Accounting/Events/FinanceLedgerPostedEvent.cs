namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// Yevmiye kaydi deftere islediginde publish edilir.
/// Exchange: mestech.mesa.finance.ledger.posted.v1
/// </summary>
public record FinanceLedgerPostedEvent(
    Guid JournalEntryId,
    decimal TotalAmount,
    string Source,
    List<string> AccountCodes,
    Guid TenantId,
    DateTime OccurredAt);
