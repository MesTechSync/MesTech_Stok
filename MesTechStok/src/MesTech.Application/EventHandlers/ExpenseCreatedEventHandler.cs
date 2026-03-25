using MesTech.Domain.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IExpenseCreatedEventHandler
{
    Task HandleAsync(ExpenseCreatedEvent domainEvent, CancellationToken ct);
}

public sealed class ExpenseCreatedEventHandler : IExpenseCreatedEventHandler
{
    private readonly ILogger<ExpenseCreatedEventHandler> _logger;

    public ExpenseCreatedEventHandler(ILogger<ExpenseCreatedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(ExpenseCreatedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Gider kaydı oluştu — ExpenseId={Id}, Title={Title}, Amount={Amount}, Source={Source}",
            domainEvent.ExpenseId, domainEvent.Title, domainEvent.Amount, domainEvent.Source);
        return Task.CompletedTask;
    }
}
