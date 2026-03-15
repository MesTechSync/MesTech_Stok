namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// Banka ekstre import tamamlandiginda publish edilir.
/// Exchange: mestech.mesa.finance.bank.imported.v1
/// </summary>
public record FinanceBankImportedEvent(
    Guid BankAccountId,
    int TransactionCount,
    decimal TotalCredits,
    decimal TotalDebits,
    DateTime ImportDate,
    Guid TenantId,
    DateTime OccurredAt);
