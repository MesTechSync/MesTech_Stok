using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Events.Crm;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Infrastructure.Messaging.Handlers;

public sealed class DealWonBridgeHandler : INotificationHandler<DomainEventNotification<DealWonEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DealWonBridgeHandler> _logger;

    public DealWonBridgeHandler(
        IMesaEventPublisher mesaPublisher, IDbContextFactory<AppDbContext> contextFactory,
        ITenantProvider tenantProvider, ILogger<DealWonBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _contextFactory = contextFactory;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<DealWonEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // Deal entity'sini DB'den çek — Title ve CrmContactId için
        var deal = await context.Set<Deal>()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == e.DealId, cancellationToken).ConfigureAwait(false);

        if (deal is null)
        {
            _logger.LogWarning("DealWonBridge: Deal {DealId} not found in DB", e.DealId);
            return;
        }

        var tenantId = _tenantProvider.GetCurrentTenantId() != Guid.Empty
            ? _tenantProvider.GetCurrentTenantId()
            : deal.TenantId;

        _logger.LogInformation(
            "DealWon bridge: Deal='{Title}' Amount={Amount} TenantId={TenantId}",
            deal.Title, e.Amount, tenantId);

        await _mesaPublisher.PublishDealWonAsync(new DealWonIntegrationEvent(
            DealId: e.DealId,
            DealTitle: deal.Title,
            Amount: e.Amount,
            OrderId: e.OrderId,
            CrmContactId: deal.CrmContactId,
            TenantId: tenantId,
            OccurredAt: e.OccurredAt
        ), cancellationToken).ConfigureAwait(false);
    }
}
