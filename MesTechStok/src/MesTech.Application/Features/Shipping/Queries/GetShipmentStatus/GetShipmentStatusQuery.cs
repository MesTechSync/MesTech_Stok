using MediatR;
using MesTech.Application.DTOs.Shipping;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Shipping.Queries.GetShipmentStatus;

public record GetShipmentStatusQuery(
    Guid TenantId,
    string TrackingNumber,
    CargoProvider Provider
) : IRequest<ShipmentStatusDto>;
