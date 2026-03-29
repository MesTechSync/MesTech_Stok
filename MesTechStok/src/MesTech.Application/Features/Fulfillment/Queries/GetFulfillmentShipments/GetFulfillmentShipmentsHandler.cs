using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentShipments;

public sealed class GetFulfillmentShipmentsHandler
    : IRequestHandler<GetFulfillmentShipmentsQuery, FulfillmentShipmentsResultDto>
{
    private readonly IFulfillmentShipmentRepository _repo;
    private readonly ILogger<GetFulfillmentShipmentsHandler> _logger;

    public GetFulfillmentShipmentsHandler(
        IFulfillmentShipmentRepository repo,
        ILogger<GetFulfillmentShipmentsHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<FulfillmentShipmentsResultDto> Handle(
        GetFulfillmentShipmentsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fulfillment shipments sorgulanıyor — Center={Center}, Status={Status}",
            request.Center, request.Status);

        var items = await _repo.GetByTenantAsync(
            request.TenantId, request.Center, request.Status,
            request.Page, request.PageSize, cancellationToken);

        var totalCount = await _repo.CountByTenantAsync(
            request.TenantId, request.Center, request.Status, cancellationToken);

        return new FulfillmentShipmentsResultDto(
            Items: items.Select(s => new FulfillmentShipmentDto(
                s.Id, s.TrackingNumber, s.Center, s.Status, s.CreatedAt, s.ItemCount)).ToList(),
            TotalCount: totalCount);
    }
}
