using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.Accounting.Queries.GetShipmentCosts;

public record GetShipmentCostsQuery(
    Guid TenantId,
    DateTime? From = null,
    DateTime? To = null) : IRequest<IReadOnlyList<ShipmentCostDto>>;

public record ShipmentCostDto(
    Guid Id,
    Guid OrderId,
    CargoProvider Provider,
    decimal Cost,
    decimal NetCost,
    string? TrackingNumber,
    DateTime ShippedAt,
    bool IsChargedToCustomer);
