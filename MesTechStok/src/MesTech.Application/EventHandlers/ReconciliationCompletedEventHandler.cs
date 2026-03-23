using MesTech.Domain.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IReconciliationCompletedEventHandler
{
    Task HandleAsync(ReconciliationCompletedEvent domainEvent, CancellationToken ct);
}

public class ReconciliationCompletedEventHandler : IReconciliationCompletedEventHandler
{
    private readonly ILogger<ReconciliationCompletedEventHandler> _logger;

    public ReconciliationCompletedEventHandler(ILogger<ReconciliationCompletedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(ReconciliationCompletedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Mutabakat tamamlandı — MatchId={Id}, Status={Status}, Confidence={Conf:P0}",
            domainEvent.MatchId, domainEvent.FinalStatus, domainEvent.Confidence);
        return Task.CompletedTask;
    }
}
