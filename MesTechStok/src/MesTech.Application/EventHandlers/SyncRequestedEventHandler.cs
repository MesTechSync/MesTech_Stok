using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Senkronizasyon talebi oluşturulduğunda loglama yapar.
/// Gelecekte: platform senkronizasyonu tetiklenecek.
/// </summary>
public interface ISyncRequestedEventHandler
{
    Task HandleAsync(SyncRequestedEvent domainEvent, CancellationToken ct);
}

public sealed class SyncRequestedEventHandler : ISyncRequestedEventHandler
{
    private readonly ILogger<SyncRequestedEventHandler> _logger;

    public SyncRequestedEventHandler(ILogger<SyncRequestedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(SyncRequestedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "SyncRequested — PlatformCode={PlatformCode}, Direction={Direction}, EntityType={EntityType}, EntityId={EntityId}, OccurredAt={OccurredAt}",
            domainEvent.PlatformCode, domainEvent.Direction, domainEvent.EntityType,
            domainEvent.EntityId, domainEvent.OccurredAt);

        // FUTURE: Trigger platform sync

        return Task.CompletedTask;
    }
}
