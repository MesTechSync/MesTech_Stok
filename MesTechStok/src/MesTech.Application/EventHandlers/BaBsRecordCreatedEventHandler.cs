using MesTech.Domain.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IBaBsRecordCreatedEventHandler
{
    Task HandleAsync(BaBsRecordCreatedEvent domainEvent, CancellationToken ct);
}

public sealed class BaBsRecordCreatedEventHandler : IBaBsRecordCreatedEventHandler
{
    private readonly ILogger<BaBsRecordCreatedEventHandler> _logger;

    public BaBsRecordCreatedEventHandler(ILogger<BaBsRecordCreatedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(BaBsRecordCreatedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Ba/Bs kayıt oluştu — Type={Type}, VKN={Vkn}, Year={Year}/{Month}, Amount={Amount}",
            domainEvent.Type, domainEvent.CounterpartyVkn, domainEvent.Year, domainEvent.Month, domainEvent.TotalAmount);
        return Task.CompletedTask;
    }
}
