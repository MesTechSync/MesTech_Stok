using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Shipping.Commands.CreateShipment;

public sealed class CreateShipmentHandler : IRequestHandler<CreateShipmentCommand, CreateShipmentResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICargoProviderFactory _cargoProviderFactory;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateShipmentHandler> _logger;

    public CreateShipmentHandler(
        IOrderRepository orderRepository,
        ICargoProviderFactory cargoProviderFactory,
        IUnitOfWork uow,
        ILogger<CreateShipmentHandler> logger)
    {
        _orderRepository = orderRepository;
        _cargoProviderFactory = cargoProviderFactory;
        _uow = uow;
        _logger = logger;
    }

    public async Task<CreateShipmentResult> Handle(
        CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
            return CreateShipmentResult.Failed($"Order {request.OrderId} not found.");

        if (order.TenantId != request.TenantId)
            return CreateShipmentResult.Failed(
                $"Order {request.OrderId} does not belong to tenant {request.TenantId}.");

        if (!string.IsNullOrEmpty(order.TrackingNumber))
            return CreateShipmentResult.Failed(
                $"Order {request.OrderId} already shipped with tracking {order.TrackingNumber}.");

        var adapter = _cargoProviderFactory.Resolve(request.CargoProvider);
        if (adapter is null)
            return CreateShipmentResult.Failed(
                $"No cargo adapter registered for provider {request.CargoProvider}.");

        var shipmentRequest = new ShipmentRequest
        {
            OrderId = request.OrderId,
            RecipientName = request.RecipientName,
            RecipientPhone = request.RecipientPhone,
            RecipientAddress = new Domain.ValueObjects.Address { Street = request.RecipientAddress },
            SenderAddress = new Domain.ValueObjects.Address(),
            Weight = request.Weight,
            Desi = 0,
            ParcelCount = 1,
            Notes = request.Notes
        };

        var result = await adapter.CreateShipmentAsync(shipmentRequest, cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            _logger.LogWarning(
                "Shipment creation failed for order {OrderId} via {Provider}: {Error}",
                request.OrderId, request.CargoProvider, result.ErrorMessage);
            return CreateShipmentResult.Failed(result.ErrorMessage ?? "Shipment creation failed.");
        }

        var trackingNumber = result.TrackingNumber ?? string.Empty;
        order.MarkAsShipped(trackingNumber, request.CargoProvider);
        await _orderRepository.UpdateAsync(order).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Shipment created for order {OrderId} via {Provider}, tracking: {Tracking}",
            request.OrderId, request.CargoProvider, trackingNumber);

        return CreateShipmentResult.Succeeded(trackingNumber, result.ShipmentId);
    }
}
