using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

public record PlatformMessageReceivedEvent(
    Guid MessageId,
    Guid TenantId,
    PlatformType Platform,
    string SenderName,
    DateTime OccurredAt
) : IDomainEvent;
