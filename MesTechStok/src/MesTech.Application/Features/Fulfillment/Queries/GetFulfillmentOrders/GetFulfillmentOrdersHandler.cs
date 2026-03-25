using MediatR;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentOrders;

/// <summary>
/// Handler: resolves the fulfillment provider via factory and queries fulfillment orders.
/// </summary>
public sealed class GetFulfillmentOrdersHandler
    : IRequestHandler<GetFulfillmentOrdersQuery, IReadOnlyList<FulfillmentOrderResult>>
{
    private readonly IFulfillmentProviderFactory _factory;
    private readonly ILogger<GetFulfillmentOrdersHandler> _logger;

    public GetFulfillmentOrdersHandler(
        IFulfillmentProviderFactory factory,
        ILogger<GetFulfillmentOrdersHandler> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<FulfillmentOrderResult>> Handle(
        GetFulfillmentOrdersQuery request, CancellationToken cancellationToken)
    {
        var provider = _factory.Resolve(request.Center)
            ?? throw new InvalidOperationException(
                $"No fulfillment provider registered for center '{request.Center}'.");

        _logger.LogInformation(
            "[GetFulfillmentOrders] Querying {Center} for orders since {Since}",
            request.Center, request.Since);

        var orders = await provider.GetFulfillmentOrdersAsync(
            request.Since, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "[GetFulfillmentOrders] {Center} returned {Count} fulfillment orders",
            request.Center, orders.Count);

        return orders;
    }
}
