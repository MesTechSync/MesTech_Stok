using MesTech.Domain.Events.Crm;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IDealWonEventHandler
{
    Task HandleAsync(DealWonEvent domainEvent, CancellationToken ct);
}

public sealed class DealWonEventHandler : IDealWonEventHandler
{
    private readonly ILogger<DealWonEventHandler> _logger;

    public DealWonEventHandler(ILogger<DealWonEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(DealWonEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "DealWon: DealId={DealId}, OrderId={OrderId}, Amount={Amount}",
            domainEvent.DealId, domainEvent.OrderId, domainEvent.Amount);

        return Task.CompletedTask;
    }
}
