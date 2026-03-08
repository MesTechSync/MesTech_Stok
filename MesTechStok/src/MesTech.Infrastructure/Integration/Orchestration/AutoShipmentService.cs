using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Orchestration;

/// <summary>
/// Otomatik gonderim orkestrasyon servisi.
/// Flow: Order yukle → kargo firma sec → gonderi olustur → platform bildir → event.
/// </summary>
public class AutoShipmentService : IAutoShipmentService
{
    private readonly ICargoProviderSelector _selector;
    private readonly ICargoProviderFactory _cargoFactory;
    private readonly IAdapterFactory _adapterFactory;
    private readonly ILogger<AutoShipmentService> _logger;

    public AutoShipmentService(
        ICargoProviderSelector selector,
        ICargoProviderFactory cargoFactory,
        IAdapterFactory adapterFactory,
        ILogger<AutoShipmentService> logger)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _cargoFactory = cargoFactory ?? throw new ArgumentNullException(nameof(cargoFactory));
        _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ShipmentResult> ProcessOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        _logger.LogInformation("AutoShipment processing order {OrderId}", orderId);

        // NOTE: Order yukleme IOrderRepository uzerinden yapilacak.
        // Dalga 3'te repository inject edilecek. Simdilik orderId-based stub.
        // DEV 1 merge sonrasi Order entity uzerinden ShipmentRequest olusturulacak.

        // Step 1: Bir dummy order olustur (repository gelene kadar)
        var order = new MesTech.Domain.Entities.Order();
        // order.Id set edilemiyor (BaseEntity kontrolu) — sadece factory uzerinden

        // Step 2: Select best cargo provider
        var provider = await _selector.SelectBestProviderAsync(order, ct).ConfigureAwait(false);
        var cargoAdapter = _cargoFactory.Resolve(provider);

        if (cargoAdapter is null)
        {
            _logger.LogError("No cargo adapter found for provider {Provider}", provider);
            return ShipmentResult.Failed($"Kargo adaptoru bulunamadi: {provider}");
        }

        // Step 3: Create shipment request
        // TEMP: Bu kisim DEV 1'in Order entity guncellemesinden sonra dolacak
        var shipmentRequest = new ShipmentRequest
        {
            OrderId = orderId,
            RecipientName = "TEMP",
            RecipientPhone = "TEMP",
            RecipientAddress = new MesTech.Domain.ValueObjects.Address(),
            SenderAddress = new MesTech.Domain.ValueObjects.Address(),
            Weight = 1,
            Desi = 1,
            ParcelCount = 1
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
        // TEMP: PlatformType.Trendyol default — Order entity'den alinacak
        var platformCode = "Trendyol"; // order.SourcePlatform?.ToString() gelecek
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
                    RaisePlatformNotificationFailed(orderId, platformCode, result.TrackingNumber, provider,
                        "Platform notification returned false");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Platform notification failed for order {OrderId}", orderId);
                // Raise event for retry queue — DO NOT rollback cargo
                RaisePlatformNotificationFailed(orderId, platformCode, result.TrackingNumber, provider, ex.Message);
            }
        }
        else
        {
            _logger.LogInformation("No IShipmentCapableAdapter for platform {Platform}, skipping notification", platformCode);
        }

        return result;
    }

    private void RaisePlatformNotificationFailed(
        Guid orderId, string platformCode, string trackingNumber,
        CargoProvider provider, string errorMessage)
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

        // TEMP: MediatR/MassTransit uzerinden publish edilecek (DEV 6 bridge integration)
        // Simdilik log ile kayit altina aliniyor.
        _logger.LogWarning("PlatformNotificationFailedEvent raised for order {OrderId}: {Error}",
            orderId, errorMessage);
    }
}
