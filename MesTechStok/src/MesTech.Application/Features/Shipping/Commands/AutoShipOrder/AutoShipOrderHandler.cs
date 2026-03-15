using MediatR;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.DTOs.Shipping;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using DomainShipmentRequest = MesTech.Domain.Services.ShipmentRequest;
using IAutoShipmentService = MesTech.Domain.Services.IAutoShipmentService;

namespace MesTech.Application.Features.Shipping.Commands.AutoShipOrder;

public class AutoShipOrderHandler : IRequestHandler<AutoShipOrderCommand, AutoShipResult>
{
    private const string CashOnDeliveryStatus = "CashOnDelivery";

    private readonly IOrderRepository _orderRepository;
    private readonly IAutoShipmentService _autoShipmentService;
    private readonly ICargoProviderFactory _cargoProviderFactory;
    private readonly IUnitOfWork _uow;

    public AutoShipOrderHandler(
        IOrderRepository orderRepository,
        IAutoShipmentService autoShipmentService,
        ICargoProviderFactory cargoProviderFactory,
        IUnitOfWork uow)
    {
        _orderRepository = orderRepository;
        _autoShipmentService = autoShipmentService;
        _cargoProviderFactory = cargoProviderFactory;
        _uow = uow;
    }

    public async Task<AutoShipResult> Handle(AutoShipOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId);

        var validationError = ValidateOrder(order, request);
        if (validationError is not null)
            return validationError;

        var recommendation = _autoShipmentService.Recommend(BuildDomainRequest(order!));

        var adapter = _cargoProviderFactory.Resolve(recommendation.Provider);
        if (adapter is null)
            return AutoShipResult.Failed(
                $"No cargo adapter registered for provider {recommendation.Provider}.",
                recommendation.Provider);

        var shipmentResult = await adapter.CreateShipmentAsync(
            BuildShipmentRequest(order!), cancellationToken);

        if (!shipmentResult.Success)
            return AutoShipResult.Failed(
                shipmentResult.ErrorMessage ?? "Shipment creation failed.",
                recommendation.Provider);

        var trackingNumber = shipmentResult.TrackingNumber ?? string.Empty;
        order!.MarkAsShipped(trackingNumber, recommendation.Provider);
        await _orderRepository.UpdateAsync(order);
        await _uow.SaveChangesAsync(cancellationToken);

        return AutoShipResult.Succeeded(
            trackingNumber,
            recommendation.Provider,
            Guid.TryParse(shipmentResult.ShipmentId, out var sid) ? sid : Guid.NewGuid(),
            recommendation.Reason);
    }

    private static AutoShipResult? ValidateOrder(Order? order, AutoShipOrderCommand request)
    {
        if (order is null)
            return AutoShipResult.Failed($"Order {request.OrderId} not found.");

        if (order.TenantId != request.TenantId)
            return AutoShipResult.Failed(
                $"Order {request.OrderId} does not belong to tenant {request.TenantId}.");

        if (!string.IsNullOrEmpty(order.TrackingNumber))
            return AutoShipResult.Failed(
                $"Order {request.OrderId} already shipped with tracking {order.TrackingNumber}.");

        return null;
    }

    private static DomainShipmentRequest BuildDomainRequest(Order order)
        => new(
            DestinationCity: order.CustomerName ?? string.Empty,
            WeightKg: 0,
            Desi: 0,
            IsCashOnDelivery: string.Equals(order.PaymentStatus, CashOnDeliveryStatus, StringComparison.Ordinal),
            SourcePlatform: order.SourcePlatform,
            OrderAmount: order.TotalAmount);

    private static ShipmentRequest BuildShipmentRequest(Order order)
        => new()
        {
            OrderId = order.Id,
            RecipientName = order.CustomerName ?? string.Empty,
            RecipientPhone = string.Empty,
            RecipientAddress = new Domain.ValueObjects.Address(),
            SenderAddress = new Domain.ValueObjects.Address(),
            Weight = 0,
            Desi = 0,
            CodAmount = string.Equals(order.PaymentStatus, CashOnDeliveryStatus, StringComparison.Ordinal)
                ? order.TotalAmount
                : null,
            ParcelCount = 1,
            Notes = order.Notes
        };
}
