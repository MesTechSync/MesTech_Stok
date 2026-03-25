using MediatR;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentInventory;

/// <summary>
/// Handler: resolves the fulfillment provider via factory and queries inventory levels.
/// </summary>
public sealed class GetFulfillmentInventoryHandler
    : IRequestHandler<GetFulfillmentInventoryQuery, FulfillmentInventory>
{
    private readonly IFulfillmentProviderFactory _factory;
    private readonly ILogger<GetFulfillmentInventoryHandler> _logger;

    public GetFulfillmentInventoryHandler(
        IFulfillmentProviderFactory factory,
        ILogger<GetFulfillmentInventoryHandler> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FulfillmentInventory> Handle(
        GetFulfillmentInventoryQuery request, CancellationToken cancellationToken)
    {
        var provider = _factory.Resolve(request.Center)
            ?? throw new InvalidOperationException(
                $"No fulfillment provider registered for center '{request.Center}'.");

        _logger.LogInformation(
            "[GetFulfillmentInventory] Querying {Center} for {SkuCount} SKUs",
            request.Center, request.Skus.Count);

        var inventory = await provider.GetInventoryLevelsAsync(
            request.Skus, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "[GetFulfillmentInventory] {Center} returned {Count} stock records",
            request.Center, inventory.Stocks.Count);

        return inventory;
    }
}
