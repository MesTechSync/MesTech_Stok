using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IProductUpdatedEventHandler
{
    Task HandleAsync(ProductUpdatedEvent domainEvent, CancellationToken ct);
}

public sealed class ProductUpdatedEventHandler : IProductUpdatedEventHandler
{
    private readonly ILogger<ProductUpdatedEventHandler> _logger;

    public ProductUpdatedEventHandler(ILogger<ProductUpdatedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(ProductUpdatedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Ürün güncellendi — ProductId={ProductId}, SKU={SKU}",
            domainEvent.ProductId, domainEvent.SKU);
        return Task.CompletedTask;
    }
}
