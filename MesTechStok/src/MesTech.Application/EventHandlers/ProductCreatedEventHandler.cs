using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IProductCreatedEventHandler
{
    Task HandleAsync(ProductCreatedEvent domainEvent, CancellationToken ct);
}

public sealed class ProductCreatedEventHandler : IProductCreatedEventHandler
{
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ProductCreatedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "ProductCreated: ProductId={ProductId}, SKU={SKU}, Name={Name}, Price={Price}",
            domainEvent.ProductId, domainEvent.SKU, domainEvent.Name, domainEvent.SalePrice);

        return Task.CompletedTask;
    }
}
