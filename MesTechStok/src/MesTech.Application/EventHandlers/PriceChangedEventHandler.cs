using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IPriceChangedEventHandler
{
    Task HandleAsync(PriceChangedEvent domainEvent, CancellationToken ct);
}

public sealed class PriceChangedEventHandler : IPriceChangedEventHandler
{
    private readonly ILogger<PriceChangedEventHandler> _logger;

    public PriceChangedEventHandler(ILogger<PriceChangedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PriceChangedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "PriceChanged: ProductId={ProductId}, SKU={SKU}, Old={Old}, New={New}",
            domainEvent.ProductId, domainEvent.SKU, domainEvent.OldPrice, domainEvent.NewPrice);

        return Task.CompletedTask;
    }
}
