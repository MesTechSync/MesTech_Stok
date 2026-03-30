using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.Shipping.Commands.CreateShipment;

public record CreateShipmentCommand(
    Guid TenantId,
    Guid OrderId,
    CargoProvider CargoProvider,
    string RecipientName,
    string RecipientAddress,
    string RecipientPhone,
    decimal Weight,
    string? Notes = null
) : IRequest<CreateShipmentResult>;
