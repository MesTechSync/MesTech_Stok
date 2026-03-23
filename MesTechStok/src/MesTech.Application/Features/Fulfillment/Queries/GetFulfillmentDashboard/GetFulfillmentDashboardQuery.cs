using MediatR;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentDashboard;

public record GetFulfillmentDashboardQuery(Guid TenantId) : IRequest<FulfillmentDashboardDto>;

public record FulfillmentDashboardDto(
    int TotalProducts,
    int FbaProducts,
    int HlProducts,
    int OwnWarehouseProducts,
    int PendingShipments,
    int InTransitShipments,
    int DeliveredToday);
