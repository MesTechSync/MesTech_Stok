using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Siparis odemesi tamamlandiginda firlatilir.
/// Handler: Fatura olusturma, GL kaydi, musteri bilgilendirme.
/// </summary>
public record OrderPaidEvent(
    Guid OrderId,
    Guid TenantId,
    string OrderNumber,
    decimal TotalAmount,
    DateTime OccurredAt
) : IDomainEvent;
