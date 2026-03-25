using MesTech.Domain.Events.Finance;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IExpensePaidEventHandler
{
    Task HandleAsync(ExpensePaidEvent domainEvent, CancellationToken ct);
}

public sealed class ExpensePaidEventHandler : IExpensePaidEventHandler
{
    private readonly ILogger<ExpensePaidEventHandler> _logger;

    public ExpensePaidEventHandler(ILogger<ExpensePaidEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ExpensePaidEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "ExpensePaid: ExpenseId={ExpenseId}, BankAccountId={BankAccountId}",
            domainEvent.ExpenseId, domainEvent.BankAccountId);

        return Task.CompletedTask;
    }
}
