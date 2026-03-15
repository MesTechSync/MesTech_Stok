namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Banka ekstresi iceye aktarildiginda tetiklenir.
/// </summary>
public record BankStatementImportedEvent : AccountingDomainEvent
{
    public Guid BankAccountId { get; init; }
    public int TransactionCount { get; init; }
    public decimal TotalInflow { get; init; }
    public decimal TotalOutflow { get; init; }
}
