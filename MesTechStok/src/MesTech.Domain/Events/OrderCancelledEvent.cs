using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Siparis iptal edildiginde firlatilir — stok iade ve muhasebe kaydini tetikler.
/// </summary>
public record OrderCancelledEvent(
    Guid OrderId,
    Guid TenantId,
    string PlatformCode,
    string PlatformOrderId,
    string? Reason,
    DateTime OccurredAt
) : IDomainEvent;
