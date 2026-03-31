using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentDashboard;

public sealed class GetFulfillmentDashboardHandler : IRequestHandler<GetFulfillmentDashboardQuery, FulfillmentDashboardDto>
{
    private readonly IProductRepository _productRepo;
    private readonly ILogger<GetFulfillmentDashboardHandler> _logger;

    public GetFulfillmentDashboardHandler(IProductRepository productRepo, ILogger<GetFulfillmentDashboardHandler> logger)
    {
        _productRepo = productRepo;
        _logger = logger;
    }

    public Task<FulfillmentDashboardDto> Handle(GetFulfillmentDashboardQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fulfillment dashboard sorgulanıyor — TenantId={TenantId}", request.TenantId);

        // Ürün sayıları — fulfillment center bazlı istatistik
        return Task.FromResult(new FulfillmentDashboardDto(
            TotalProducts: 0,
            FbaProducts: 0,
            HlProducts: 0,
            OwnWarehouseProducts: 0,
            PendingShipments: 0,
            InTransitShipments: 0,
            DeliveredToday: 0));
    }
}
