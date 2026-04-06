using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Iade urun teslim alindiginda firlatilir.
/// Handler: Iade kalite kontrol, stok sayim, refund workflow tetikle.
/// </summary>
public record ReturnReceivedEvent(
    Guid ReturnRequestId,
    Guid OrderId,
    Guid TenantId,
    DateTime OccurredAt) : IDomainEvent;
