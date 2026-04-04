using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Iade talebi reddedildiginde firlatilir.
/// Handler: Musteri bilgilendirme, CRM kaydi.
/// </summary>
public record ReturnRejectedEvent(
    Guid ReturnRequestId,
    Guid OrderId,
    Guid TenantId,
    string? Reason,
    DateTime OccurredAt) : IDomainEvent;
