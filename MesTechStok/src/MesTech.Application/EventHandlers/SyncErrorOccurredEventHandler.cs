using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface ISyncErrorOccurredEventHandler
{
    Task HandleAsync(SyncErrorOccurredEvent domainEvent, CancellationToken ct);
}

public class SyncErrorOccurredEventHandler : ISyncErrorOccurredEventHandler
{
    private readonly ILogger<SyncErrorOccurredEventHandler> _logger;

    public SyncErrorOccurredEventHandler(ILogger<SyncErrorOccurredEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(SyncErrorOccurredEvent domainEvent, CancellationToken ct)
    {
        _logger.LogError(
            "SyncError: Platform={Platform}, ErrorType={ErrorType}, Message={Message}",
            domainEvent.Platform, domainEvent.ErrorType, domainEvent.Message);

        return Task.CompletedTask;
    }
}
