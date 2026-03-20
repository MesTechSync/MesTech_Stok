using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

/// <summary>
/// Kullanici bildirim ayarlari guncellediginde firlatilir.
/// </summary>
public record NotificationSettingsUpdatedEvent(
    Guid UserId,
    NotificationChannel Channel,
    bool IsEnabled,
    DateTime OccurredAt
) : IDomainEvent;
