using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentDashboard;

public sealed class GetFulfillmentDashboardHandler : IRequestHandler<GetFulfillmentDashboardQuery, FulfillmentDashboardDto>
{
    private readonly IProductRepository _productRepo;
    private readonly IFulfillmentShipmentRepository _shipmentRepo;
    private readonly ILogger<GetFulfillmentDashboardHandler> _logger;

    public GetFulfillmentDashboardHandler(
        IProductRepository productRepo,
        IFulfillmentShipmentRepository shipmentRepo,
        ILogger<GetFulfillmentDashboardHandler> logger)
    {
        _productRepo = productRepo;
        _shipmentRepo = shipmentRepo;
        _logger = logger;
    }

    public async Task<FulfillmentDashboardDto> Handle(GetFulfillmentDashboardQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fulfillment dashboard sorgulanıyor — TenantId={TenantId}", request.TenantId);

        var totalProducts = await _productRepo.CountByTenantAsync(request.TenantId, cancellationToken);

        var pendingShipments = await _shipmentRepo.CountByTenantAsync(
            request.TenantId, status: "Pending", ct: cancellationToken);
        var inTransitShipments = await _shipmentRepo.CountByTenantAsync(
            request.TenantId, status: "InTransit", ct: cancellationToken);
        var deliveredToday = await _shipmentRepo.CountByTenantAsync(
            request.TenantId, status: "Delivered", ct: cancellationToken);

        var fbaCount = await _shipmentRepo.CountByTenantAsync(
            request.TenantId, center: "AmazonFBA", ct: cancellationToken);
        var hlCount = await _shipmentRepo.CountByTenantAsync(
            request.TenantId, center: "Hepsilojistik", ct: cancellationToken);

        return new FulfillmentDashboardDto(
            TotalProducts: totalProducts,
            FbaProducts: fbaCount,
            HlProducts: hlCount,
            OwnWarehouseProducts: totalProducts - fbaCount - hlCount,
            PendingShipments: pendingShipments,
            InTransitShipments: inTransitShipments,
            DeliveredToday: deliveredToday);
    }
}
