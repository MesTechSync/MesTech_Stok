using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentShipments;

public sealed class GetFulfillmentShipmentsHandler
    : IRequestHandler<GetFulfillmentShipmentsQuery, FulfillmentShipmentsResultDto>
{
    private readonly ILogger<GetFulfillmentShipmentsHandler> _logger;

    public GetFulfillmentShipmentsHandler(ILogger<GetFulfillmentShipmentsHandler> logger)
        => _logger = logger;

    public Task<FulfillmentShipmentsResultDto> Handle(
        GetFulfillmentShipmentsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fulfillment shipments sorgulanıyor — Center={Center}, Status={Status}",
            request.Center, request.Status);

        return Task.FromResult(new FulfillmentShipmentsResultDto(
            Items: Array.Empty<FulfillmentShipmentDto>(), TotalCount: 0));
    }
}
