namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Platform hesap kesimi mutabakat tamamlandiginda tetiklenir.
/// </summary>
public record SettlementReconciledEvent : AccountingDomainEvent
{
    public Guid SettlementBatchId { get; init; }
    public string Platform { get; init; } = string.Empty;
    public decimal TotalNet { get; init; }
}
