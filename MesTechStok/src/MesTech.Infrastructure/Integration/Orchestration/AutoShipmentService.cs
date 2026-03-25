using MediatR;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Orchestration;

/// <summary>
/// Otomatik gonderim orkestrasyon servisi.
/// Flow: Order yukle → kargo firma sec → gonderi olustur → platform bildir → event.
/// Pazarama 2-stage (status 12→5) PazaramaAdapter.SendShipmentAsync icinde handle edilir,
/// bu servis tum platformlara ayni sekilde davranir.
/// </summary>
public sealed class AutoShipmentService : IAutoShipmentService
{
    private readonly ICargoProviderSelector _selector;
    private readonly ICargoProviderFactory _cargoFactory;
    private readonly IAdapterFactory _adapterFactory;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<AutoShipmentService> _logger;
    private readonly IPublisher? _publisher;

    public AutoShipmentService(
        ICargoProviderSelector selector,
        ICargoProviderFactory cargoFactory,
        IAdapterFactory adapterFactory,
        IOrderRepository orderRepository,
        ILogger<AutoShipmentService> logger,
        IPublisher? publisher = null)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _cargoFactory = cargoFactory ?? throw new ArgumentNullException(nameof(cargoFactory));
        _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publisher = publisher;
    }

    public async Task<ShipmentResult> ProcessOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        _logger.LogInformation("AutoShipment processing order {OrderId}", orderId);

        // Step 1: Load order from repository
        var order = await _orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);

        if (order is null)
        {
            _logger.LogError("Order {OrderId} not found", orderId);
            return ShipmentResult.Failed($"Siparis bulunamadi: {orderId}");
        }

        // Step 2: Select best cargo provider
        var provider = await _selector.SelectBestProviderAsync(order, ct).ConfigureAwait(false);
        var cargoAdapter = _cargoFactory.Resolve(provider);

        if (cargoAdapter is null)
        {
            _logger.LogError("No cargo adapter found for provider {Provider}", provider);
            return ShipmentResult.Failed($"Kargo adaptoru bulunamadi: {provider}");
        }

        // Step 3: Create shipment request from order data
        var recipientName = order.CustomerName;
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            _logger.LogWarning("Order {OrderId} has no CustomerName, using fallback", orderId);
            recipientName = "N/A";
        }

        var shipmentRequest = new ShipmentRequest
        {
            OrderId = orderId,
            RecipientName = recipientName,
            RecipientPhone = "N/A", // Order entity does not yet have phone field — Dalga 5 scope
            RecipientAddress = new MesTech.Domain.ValueObjects.Address(),
            SenderAddress = new MesTech.Domain.ValueObjects.Address(),
            Weight = 1,
            Desi = 1,
            ParcelCount = order.TotalItems > 0 ? order.TotalItems : 1
        };

        // Step 4: Create shipment
        var result = await cargoAdapter.CreateShipmentAsync(shipmentRequest, ct).ConfigureAwait(false);

        if (!result.Success)
        {
            _logger.LogWarning("Cargo shipment creation failed for order {OrderId}: {Error}",
                orderId, result.ErrorMessage);
            return result;
        }

        _logger.LogInformation("Cargo shipment created for order {OrderId}: tracking={Tracking}",
            orderId, result.TrackingNumber);

        // Step 5: Notify platform (IShipmentCapableAdapter)
        // PlatformCode resolved from Order.SourcePlatform — all platforms (incl. Pazarama) use same flow
        var platformCode = order.SourcePlatform?.ToString() ?? "Trendyol";
        if (order.SourcePlatform is null)
        {
            _logger.LogWarning("Order {OrderId} has no SourcePlatform, defaulting to Trendyol", orderId);
        }

        var shipmentAdapter = _adapterFactory.ResolveCapability<IShipmentCapableAdapter>(platformCode);

        if (shipmentAdapter is not null && result.TrackingNumber is not null)
        {
            try
            {
                var notified = await shipmentAdapter.SendShipmentAsync(
                    orderId.ToString(), result.TrackingNumber, provider, ct).ConfigureAwait(false);

                if (!notified)
                {
                    _logger.LogWarning("Platform notification returned false for order {OrderId}", orderId);
                    // Raise event for retry queue — DO NOT rollback cargo
                    await RaisePlatformNotificationFailedAsync(orderId, platformCode,
                        result.TrackingNumber, provider, "Platform notification returned false", ct)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Platform notification failed for order {OrderId}", orderId);
                // Raise event for retry queue — DO NOT rollback cargo
                await RaisePlatformNotificationFailedAsync(orderId, platformCode,
                    result.TrackingNumber, provider, ex.Message, ct)
                    .ConfigureAwait(false);
            }
        }
        else
        {
            _logger.LogInformation("No IShipmentCapableAdapter for platform {Platform}, skipping notification", platformCode);
        }

        return result;
    }

    private async Task RaisePlatformNotificationFailedAsync(
        Guid orderId, string platformCode, string trackingNumber,
        CargoProvider provider, string errorMessage, CancellationToken ct)
    {
        var failedEvent = new PlatformNotificationFailedEvent
        {
            OrderId = orderId,
            PlatformCode = platformCode,
            TrackingNumber = trackingNumber,
            CargoProvider = provider,
            ErrorMessage = errorMessage,
            RetryCount = 0
        };

        _logger.LogWarning("PlatformNotificationFailedEvent raised for order {OrderId}: {Error}",
            orderId, errorMessage);

        if (_publisher is not null)
        {
            var notification = new DomainEventNotification<PlatformNotificationFailedEvent>(failedEvent);
            await _publisher.Publish(notification, ct).ConfigureAwait(false);
            _logger.LogInformation("PlatformNotificationFailedEvent published via MediatR for order {OrderId}", orderId);
        }
    }
}
