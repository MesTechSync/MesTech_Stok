using MediatR;
using MesTech.Application.DTOs.Shipping;

namespace MesTech.Application.Features.Shipping.Commands.BatchShipOrders;

public record BatchShipOrdersCommand(
    Guid TenantId,
    List<Guid> OrderIds
) : IRequest<BatchShipResult>;
