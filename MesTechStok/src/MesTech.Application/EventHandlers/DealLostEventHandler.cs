using MesTech.Domain.Events.Crm;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IDealLostEventHandler
{
    Task HandleAsync(DealLostEvent domainEvent, CancellationToken ct);
}

public class DealLostEventHandler : IDealLostEventHandler
{
    private readonly ILogger<DealLostEventHandler> _logger;

    public DealLostEventHandler(ILogger<DealLostEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(DealLostEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "DealLost: DealId={DealId}, Reason={Reason}",
            domainEvent.DealId, domainEvent.Reason);

        return Task.CompletedTask;
    }
}
