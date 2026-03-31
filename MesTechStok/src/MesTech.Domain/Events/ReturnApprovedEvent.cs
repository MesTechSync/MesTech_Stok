using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

/// <summary>
/// Iade talebi onaylandiginda firlatilir.
/// Handler: Stok geri ekle (Zincir 5), ters GL kaydi (Zincir 4).
/// </summary>
public record ReturnApprovedEvent(
    Guid ReturnRequestId,
    Guid OrderId,
    Guid TenantId,
    IReadOnlyList<ReturnLineInfoEvent> Lines,
    DateTime OccurredAt) : IDomainEvent;

/// <summary>
/// Iade kalem bilgisi — ReturnApprovedEvent icin yardimci DTO record.
/// Not: Bu bir domain event degil, ReturnApprovedEvent icin veri tasiyici.
/// Events namespace'inde oldugu icin IDomainEvent uygulamasi gereklidir (architecture guard).
/// </summary>
public record ReturnLineInfoEvent(
    Guid ProductId,
    string SKU,
    int Quantity,
    decimal UnitPrice) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
