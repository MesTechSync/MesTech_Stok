using MediatR;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Fulfillment.Commands.CreateInboundShipment;

/// <summary>
/// Handler: resolves the fulfillment provider via factory and creates an inbound shipment.
/// </summary>
public class CreateInboundShipmentHandler
    : IRequestHandler<CreateInboundShipmentCommand, InboundResult>
{
    private readonly IFulfillmentProviderFactory _factory;
    private readonly ILogger<CreateInboundShipmentHandler> _logger;

    public CreateInboundShipmentHandler(
        IFulfillmentProviderFactory factory,
        ILogger<CreateInboundShipmentHandler> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<InboundResult> Handle(
        CreateInboundShipmentCommand request, CancellationToken cancellationToken)
    {
        var provider = _factory.Resolve(request.Center)
            ?? throw new InvalidOperationException(
                $"No fulfillment provider registered for center '{request.Center}'.");

        _logger.LogInformation(
            "[CreateInboundShipment] Creating shipment '{Name}' at {Center} with {ItemCount} items",
            request.ShipmentName, request.Center, request.Items.Count);

        var shipmentRequest = new InboundShipmentRequest(
            request.ShipmentName,
            request.Center,
            request.Items,
            request.ExpectedArrival,
            request.Notes);

        var result = await provider.CreateInboundShipmentAsync(
            shipmentRequest, cancellationToken).ConfigureAwait(false);

        if (result.Success)
        {
            _logger.LogInformation(
                "[CreateInboundShipment] Shipment created successfully: {ShipmentId}",
                result.ShipmentId);
        }
        else
        {
            _logger.LogWarning(
                "[CreateInboundShipment] Shipment creation failed: {Error}",
                result.ErrorMessage);
        }

        return result;
    }
}
