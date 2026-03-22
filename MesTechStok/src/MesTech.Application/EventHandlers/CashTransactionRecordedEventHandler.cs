using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Kasa hareketi kaydedildiğinde loglama yapar.
/// </summary>
public interface ICashTransactionRecordedEventHandler
{
    Task HandleAsync(CashTransactionRecordedEvent domainEvent, CancellationToken ct);
}

public class CashTransactionRecordedEventHandler : ICashTransactionRecordedEventHandler
{
    private readonly ILogger<CashTransactionRecordedEventHandler> _logger;

    public CashTransactionRecordedEventHandler(ILogger<CashTransactionRecordedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(CashTransactionRecordedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "CashTransactionRecorded — TenantId={TenantId}, CashRegisterId={CashRegisterId}, TransactionId={TransactionId}, Type={Type}, Amount={Amount}, NewBalance={NewBalance}, OccurredAt={OccurredAt}",
            domainEvent.TenantId, domainEvent.CashRegisterId, domainEvent.TransactionId,
            domainEvent.Type, domainEvent.Amount, domainEvent.NewBalance, domainEvent.OccurredAt);

        return Task.CompletedTask;
    }
}
