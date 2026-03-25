using MesTech.Domain.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface ITaxWithholdingComputedEventHandler
{
    Task HandleAsync(TaxWithholdingComputedEvent domainEvent, CancellationToken ct);
}

public sealed class TaxWithholdingComputedEventHandler : ITaxWithholdingComputedEventHandler
{
    private readonly ILogger<TaxWithholdingComputedEventHandler> _logger;

    public TaxWithholdingComputedEventHandler(ILogger<TaxWithholdingComputedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(TaxWithholdingComputedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Stopaj hesaplandı — Id={Id}, TaxType={Type}, Rate={Rate:P2}, Amount={Amount}",
            domainEvent.TaxWithholdingId, domainEvent.TaxType, domainEvent.Rate, domainEvent.WithholdingAmount);
        return Task.CompletedTask;
    }
}
