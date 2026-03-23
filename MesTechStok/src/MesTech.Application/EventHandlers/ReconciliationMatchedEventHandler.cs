using MesTech.Domain.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IReconciliationMatchedEventHandler
{
    Task HandleAsync(ReconciliationMatchedEvent domainEvent, CancellationToken ct);
}

public class ReconciliationMatchedEventHandler : IReconciliationMatchedEventHandler
{
    private readonly ILogger<ReconciliationMatchedEventHandler> _logger;

    public ReconciliationMatchedEventHandler(ILogger<ReconciliationMatchedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(ReconciliationMatchedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Mutabakat eşleştirmesi — MatchId={Id}, BankTx={BankId}, Settlement={SettId}, Confidence={Conf:P0}",
            domainEvent.ReconciliationMatchId, domainEvent.BankTransactionId, domainEvent.SettlementBatchId, domainEvent.Confidence);
        return Task.CompletedTask;
    }
}
