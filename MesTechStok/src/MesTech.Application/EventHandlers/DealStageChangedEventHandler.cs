using MesTech.Domain.Events.Crm;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// CRM fırsat aşaması değiştiğinde loglama yapar.
/// </summary>
public interface IDealStageChangedEventHandler
{
    Task HandleAsync(DealStageChangedEvent domainEvent, CancellationToken ct);
}

public sealed class DealStageChangedEventHandler : IDealStageChangedEventHandler
{
    private readonly ILogger<DealStageChangedEventHandler> _logger;

    public DealStageChangedEventHandler(ILogger<DealStageChangedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(DealStageChangedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "DealStageChanged — DealId={DealId}, FromStageId={FromStageId}, ToStageId={ToStageId}, OccurredAt={OccurredAt}",
            domainEvent.DealId, domainEvent.FromStageId, domainEvent.ToStageId, domainEvent.OccurredAt);

        return Task.CompletedTask;
    }
}
