using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Events.Crm;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Infrastructure.Messaging.Handlers;

public class DealLostBridgeHandler : INotificationHandler<DomainEventNotification<DealLostEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DealLostBridgeHandler> _logger;

    public DealLostBridgeHandler(
        IMesaEventPublisher mesaPublisher, AppDbContext context,
        ITenantProvider tenantProvider, ILogger<DealLostBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<DealLostEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        // Deal entity'sini DB'den cek — Title ve Amount icin
        var deal = await _context.Set<Deal>()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == e.DealId, cancellationToken).ConfigureAwait(false);

        if (deal is null)
        {
            _logger.LogWarning("DealLostBridge: Deal {DealId} not found in DB", e.DealId);
            return;
        }

        var tenantId = _tenantProvider.GetCurrentTenantId() != Guid.Empty
            ? _tenantProvider.GetCurrentTenantId()
            : deal.TenantId;

        _logger.LogInformation(
            "DealLost bridge: Deal='{Title}' Reason={Reason} Amount={Amount} TenantId={TenantId}",
            deal.Title, e.Reason, deal.Amount, tenantId);

        await _mesaPublisher.PublishDealLostAsync(new DealLostIntegrationEvent(
            DealId: e.DealId,
            DealTitle: deal.Title,
            Reason: e.Reason,
            Amount: deal.Amount,
            TenantId: tenantId,
            OccurredAt: e.OccurredAt
        ), cancellationToken).ConfigureAwait(false);
    }
}
