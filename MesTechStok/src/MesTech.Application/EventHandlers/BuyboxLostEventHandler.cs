using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IBuyboxLostEventHandler
{
    Task HandleAsync(BuyboxLostEvent domainEvent, CancellationToken ct);
}

public sealed class BuyboxLostEventHandler : IBuyboxLostEventHandler
{
    private readonly ILogger<BuyboxLostEventHandler> _logger;

    public BuyboxLostEventHandler(ILogger<BuyboxLostEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(BuyboxLostEvent domainEvent, CancellationToken ct)
    {
        _logger.LogWarning(
            "BuyboxLost: ProductId={ProductId}, SKU={SKU}, OurPrice={Our}, CompetitorPrice={Competitor}, Competitor={Name}",
            domainEvent.ProductId, domainEvent.SKU, domainEvent.CurrentPrice,
            domainEvent.CompetitorPrice, domainEvent.CompetitorName);

        return Task.CompletedTask;
    }
}
