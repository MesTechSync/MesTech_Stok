using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

public record SyncRequestedEvent(
    string PlatformCode,
    SyncDirection Direction,
    string EntityType,
    string? EntityId,
    DateTime OccurredAt
) : IDomainEvent;
