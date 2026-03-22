using MesTech.Application.Interfaces;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Orchestration;

/// <summary>
/// Stok sıfıra düştüğünde TÜM platformlarda ürünü pasife alır (stok=0 gönderir).
/// Zincir 8: ZeroStockDetectedEvent → tüm aktif platformlarda stok 0 push.
///
/// Overselling koruması — stok yoksa satışa AÇIK OLMAMALI.
/// Her platform çağrısı bağımsız try-catch — bir fail diğerlerini engellemez.
/// </summary>
public sealed class ZeroStockPlatformDeactivationHandler
    : INotificationHandler<DomainEventNotification<ZeroStockDetectedEvent>>
{
    private readonly IIntegratorOrchestrator _orchestrator;
    private readonly IAdapterFactory _adapterFactory;
    private readonly ILogger<ZeroStockPlatformDeactivationHandler> _logger;

    public ZeroStockPlatformDeactivationHandler(
        IIntegratorOrchestrator orchestrator,
        IAdapterFactory adapterFactory,
        ILogger<ZeroStockPlatformDeactivationHandler> logger)
    {
        _orchestrator = orchestrator;
        _adapterFactory = adapterFactory;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<ZeroStockDetectedEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;

        _logger.LogWarning(
            "🔴 STOK SIFIR — SKU={SKU}, ProductId={ProductId}, Önceki={Prev}. " +
            "Tüm platformlarda pasife alınıyor.",
            evt.SKU, evt.ProductId, evt.PreviousStock);

        // Tüm kayıtlı adapter'larda stok=0 gönder
        var adapters = _adapterFactory.GetAll()
            .Where(a => a.SupportsStockUpdate);

        var tasks = adapters.Select(adapter => DeactivateOnPlatformAsync(adapter, evt, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task DeactivateOnPlatformAsync(
        IIntegratorAdapter adapter,
        ZeroStockDetectedEvent evt,
        CancellationToken ct)
    {
        try
        {
            // Stok 0 gönder — çoğu platform otomatik pasife alır
            await adapter.PushStockUpdateAsync(evt.ProductId, 0, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Platform pasif — {Platform} SKU={SKU} stok=0 gönderildi",
                adapter.PlatformCode, evt.SKU);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Platform pasif FAIL — {Platform} SKU={SKU}. " +
                "⚠️ DİKKAT: Bu platform hâlâ satışta olabilir!",
                adapter.PlatformCode, evt.SKU);
        }
    }
}
