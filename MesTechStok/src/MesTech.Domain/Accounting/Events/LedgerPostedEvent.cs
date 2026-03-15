namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Yevmiye kaydi deftere islendiginde tetiklenir.
/// </summary>
public record LedgerPostedEvent : AccountingDomainEvent
{
    public Guid JournalEntryId { get; init; }
    public DateTime EntryDate { get; init; }
    public decimal TotalAmount { get; init; }
    public int LineCount { get; init; }
}
