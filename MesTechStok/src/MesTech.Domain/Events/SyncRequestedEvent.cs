using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

public record SyncRequestedEvent(
    Guid TenantId,
    string PlatformCode,
    SyncDirection Direction,
    string EntityType,
    string? EntityId,
    DateTime OccurredAt
) : IDomainEvent;
