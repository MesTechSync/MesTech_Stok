using MesTech.Domain.Accounting.Enums;

namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Yeni gider kaydi olusturuldugunda tetiklenir.
/// </summary>
public record ExpenseCreatedEvent : AccountingDomainEvent
{
    public Guid ExpenseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public ExpenseSource Source { get; init; }
}
