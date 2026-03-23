using MesTech.Domain.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IBankStatementImportedEventHandler
{
    Task HandleAsync(BankStatementImportedEvent domainEvent, CancellationToken ct);
}

public class BankStatementImportedEventHandler : IBankStatementImportedEventHandler
{
    private readonly ILogger<BankStatementImportedEventHandler> _logger;

    public BankStatementImportedEventHandler(ILogger<BankStatementImportedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(BankStatementImportedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Banka ekstresi içe aktarıldı — BankAccountId={Id}, TxCount={Count}, Inflow={In}, Outflow={Out}",
            domainEvent.BankAccountId, domainEvent.TransactionCount, domainEvent.TotalInflow, domainEvent.TotalOutflow);
        return Task.CompletedTask;
    }
}
