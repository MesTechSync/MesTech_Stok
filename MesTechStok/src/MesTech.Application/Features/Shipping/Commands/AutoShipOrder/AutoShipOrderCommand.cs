using MediatR;
using MesTech.Application.DTOs.Shipping;

namespace MesTech.Application.Features.Shipping.Commands.AutoShipOrder;

public record AutoShipOrderCommand(
    Guid TenantId,
    Guid OrderId,
    bool AllowManualOverride = false
) : IRequest<AutoShipResult>;
