using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Platform kargo bildirimi başarısız olduğunda loglama ve uyarı üretir.
/// Retry mekanizması Hangfire (DEV3 alanı) tarafından tetiklenir.
/// </summary>
public interface IPlatformNotificationFailedEventHandler
{
    Task HandleAsync(PlatformNotificationFailedEvent domainEvent, CancellationToken ct);
}

public class PlatformNotificationFailedEventHandler : IPlatformNotificationFailedEventHandler
{
    private readonly ILogger<PlatformNotificationFailedEventHandler> _logger;

    public PlatformNotificationFailedEventHandler(ILogger<PlatformNotificationFailedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PlatformNotificationFailedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogWarning(
            "Platform bildirim başarısız — OrderId={OrderId}, Platform={Platform}, Tracking={Tracking}, " +
            "CargoProvider={CargoProvider}, Retry={RetryCount}, Error={Error}",
            domainEvent.OrderId,
            domainEvent.PlatformCode,
            domainEvent.TrackingNumber,
            domainEvent.CargoProvider,
            domainEvent.RetryCount,
            domainEvent.ErrorMessage);

        return Task.CompletedTask;
    }
}
