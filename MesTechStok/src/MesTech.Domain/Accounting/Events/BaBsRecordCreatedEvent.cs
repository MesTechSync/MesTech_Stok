using MesTech.Domain.Accounting.Enums;

namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Ba/Bs kaydi olusturuldugunda tetiklenir.
/// Bildirim takvimi ve raporlama icin kullanilir.
/// </summary>
public record BaBsRecordCreatedEvent : AccountingDomainEvent
{
    public Guid BaBsRecordId { get; init; }
    public BaBsType Type { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public string CounterpartyVkn { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
}
