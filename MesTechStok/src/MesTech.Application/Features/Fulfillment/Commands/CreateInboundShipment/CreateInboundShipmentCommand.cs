using MediatR;
using MesTech.Application.DTOs.Fulfillment;

namespace MesTech.Application.Features.Fulfillment.Commands.CreateInboundShipment;

/// <summary>
/// Belirtilen fulfillment merkezine inbound sevkiyat olusturur.
/// </summary>
public record CreateInboundShipmentCommand(
    FulfillmentCenter Center,
    string ShipmentName,
    IReadOnlyList<InboundItem> Items,
    DateTime? ExpectedArrival = null,
    string? Notes = null
) : IRequest<InboundResult>;
