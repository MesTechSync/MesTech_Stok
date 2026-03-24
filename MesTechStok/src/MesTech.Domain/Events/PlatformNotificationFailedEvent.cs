using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

/// <summary>
/// Platform'a kargo bildirimi basarisiz oldugunda firlanan event.
/// Hangfire retry queue ile tekrar denenir.
/// Kargo iptal edilMEZ — sadece platform bildirimi tekrar denenir.
/// </summary>
public sealed class PlatformNotificationFailedEvent : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid TenantId { get; init; }
    public Guid OrderId { get; init; }
    public string PlatformCode { get; init; } = string.Empty;
    public string TrackingNumber { get; init; } = string.Empty;
    public CargoProvider CargoProvider { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public int RetryCount { get; init; }
}
