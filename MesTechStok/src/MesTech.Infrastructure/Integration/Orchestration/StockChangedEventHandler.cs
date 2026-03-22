using MesTech.Application.Interfaces;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Orchestration;

/// <summary>
/// Stok değiştiğinde tüm aktif platformlara sync push yapar.
/// DEV 1'in Product.AdjustStock() → StockChangedEvent → DomainEventNotification wrapper → buraya düşer.
///
/// Zincir 9: StockChangedEvent → tüm platformlarda stok güncelle.
/// Bağlantı: IIntegratorOrchestrator.HandleStockChangedAsync() → adapter.PushStockUpdateAsync()
///
/// Pattern: DomainEventNotification{T} wrapper — IDomainEvent INotification implement etmez,
/// DomainEventDispatcher wrapper ile sarar.
/// </summary>
public sealed class StockChangedEventHandler
    : INotificationHandler<DomainEventNotification<StockChangedEvent>>
{
    private readonly IIntegratorOrchestrator _orchestrator;
    private readonly ILogger<StockChangedEventHandler> _logger;

    public StockChangedEventHandler(
        IIntegratorOrchestrator orchestrator,
        ILogger<StockChangedEventHandler> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<StockChangedEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;

        _logger.LogInformation(
            "StockChangedEvent alındı — SKU={SKU}, Önceki={Prev}, Yeni={New}, Tip={Type}",
            evt.SKU,
            evt.PreviousQuantity,
            evt.NewQuantity,
            evt.MovementType);

        try
        {
            await _orchestrator.HandleStockChangedAsync(evt, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Stok sync tamamlandı — SKU={SKU}, Yeni={New}",
                evt.SKU, evt.NewQuantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Stok sync orchestrator hatası — SKU={SKU}. Platform sync'ler etkilenmiş olabilir.",
                evt.SKU);
        }
    }
}
