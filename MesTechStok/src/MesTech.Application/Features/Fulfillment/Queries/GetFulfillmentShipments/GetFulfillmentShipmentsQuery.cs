using MediatR;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentShipments;

public record GetFulfillmentShipmentsQuery(
    Guid TenantId,
    string? Center = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<FulfillmentShipmentsResultDto>;

public record FulfillmentShipmentDto(
    Guid Id, string TrackingNumber, string Center,
    string Status, DateTime CreatedAt, int ItemCount);

public record FulfillmentShipmentsResultDto(
    IReadOnlyList<FulfillmentShipmentDto> Items, int TotalCount);
