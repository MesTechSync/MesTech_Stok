namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Stopaj hesaplamasi yapildiginda tetiklenir.
/// </summary>
public record TaxWithholdingComputedEvent : AccountingDomainEvent
{
    public Guid TaxWithholdingId { get; init; }
    public decimal TaxExclusiveAmount { get; init; }
    public decimal Rate { get; init; }
    public decimal WithholdingAmount { get; init; }
    public string TaxType { get; init; } = string.Empty;
}
