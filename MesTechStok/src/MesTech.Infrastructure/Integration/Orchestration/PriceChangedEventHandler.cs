using MesTech.Application.Interfaces;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using MediatR;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1711 // DDD event handler — INotificationHandler<DomainEventNotification<T>> pattern

namespace MesTech.Infrastructure.Integration.Orchestration;

/// <summary>
/// Fiyat değiştiğinde tüm aktif platformlara sync push yapar.
/// DEV 1'in Product.UpdatePrice() → PriceChangedEvent → DomainEventNotification wrapper → buraya düşer.
///
/// Zincir: PriceChangedEvent → IIntegratorOrchestrator.HandlePriceChangedAsync() → adapter.PushPriceUpdateAsync()
/// </summary>
public sealed class PriceChangedEventHandler
    : INotificationHandler<DomainEventNotification<PriceChangedEvent>>
{
    private readonly IIntegratorOrchestrator _orchestrator;
    private readonly ILogger<PriceChangedEventHandler> _logger;

    public PriceChangedEventHandler(
        IIntegratorOrchestrator orchestrator,
        ILogger<PriceChangedEventHandler> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<PriceChangedEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;

        _logger.LogInformation(
            "PriceChangedEvent alındı — SKU={SKU}, Eski={Old}, Yeni={New}",
            evt.SKU,
            evt.OldPrice,
            evt.NewPrice);

        try
        {
            await _orchestrator.HandlePriceChangedAsync(evt, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Fiyat sync tamamlandı — SKU={SKU}, Yeni={New}",
                evt.SKU, evt.NewPrice);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Fiyat sync orchestrator hatası — SKU={SKU}. Platform sync'ler etkilenmiş olabilir.",
                evt.SKU);
        }
    }
}
