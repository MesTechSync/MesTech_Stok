using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Orchestration;

/// <summary>
/// Kargo gönderimi sonrası ShipmentCostRecordedEvent yayınlar.
/// Zincir 7 trigger noktası — DEV 1'in ShipmentCostJournalHandler'ı bu event'i dinler.
///
/// Kullanım: Kargo oluşturan servisler (AutoShipmentService, WebApi endpoint)
/// bu publisher'ı çağırarak maliyet kaydını tetikler.
///
/// Adapter'lara IMediator inject etmek yerine bu ayrı servis tercih edildi:
/// - 7 adapter'ı değiştirmek yüksek risk
/// - Adapter'lar sadece kargo API'si ile konuşmalı (SRP)
/// - Event yayınlama orchestration katmanında kalmalı
/// </summary>
public sealed class ShipmentCostEventPublisher
{
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<ShipmentCostEventPublisher> _logger;

    public ShipmentCostEventPublisher(
        IDomainEventDispatcher eventDispatcher,
        ILogger<ShipmentCostEventPublisher> logger)
    {
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Kargo gönderimi başarılı olduktan sonra çağrılır.
    /// ShipmentCostRecordedEvent fırlatır → Zincir 7 → gider yevmiye kaydı.
    /// </summary>
    public async Task PublishCostEventAsync(
        Guid orderId,
        Guid tenantId,
        string trackingNumber,
        string cargoProvider,
        decimal shippingCost,
        CancellationToken ct = default)
    {
        if (shippingCost <= 0)
        {
            _logger.LogDebug(
                "Kargo maliyeti 0 veya negatif — event atlanıyor. Order={OrderId}, Provider={Provider}",
                orderId, cargoProvider);
            return;
        }

        var evt = new ShipmentCostRecordedEvent(
            orderId, tenantId, trackingNumber, cargoProvider, shippingCost, DateTime.UtcNow);

        _logger.LogInformation(
            "ShipmentCostRecordedEvent yayınlanıyor — Order={OrderId}, Provider={Provider}, Cost={Cost:C}",
            orderId, cargoProvider, shippingCost);

        await _eventDispatcher.DispatchAsync(new[] { evt }, ct).ConfigureAwait(false);
    }
}
