using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Events.Crm;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// Lead convert olunca MESA AI'ya bildir — AI lead skoru ve öneri üretebilir.
/// </summary>
public class LeadConvertedBridgeHandler : INotificationHandler<DomainEventNotification<LeadConvertedEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<LeadConvertedBridgeHandler> _logger;

    public LeadConvertedBridgeHandler(
        IMesaEventPublisher mesaPublisher, AppDbContext context, ITenantProvider tenantProvider,
        ILogger<LeadConvertedBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<LeadConvertedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var lead = await _context.Set<Lead>()
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == e.LeadId, cancellationToken).ConfigureAwait(false);

        var tenantId = _tenantProvider.GetCurrentTenantId() != Guid.Empty
            ? _tenantProvider.GetCurrentTenantId()
            : lead?.TenantId ?? Guid.Empty;

        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning(
                "[LeadConvertedBridge] TenantId could not be resolved for LeadId={LeadId} — " +
                "event will be published with empty tenant", e.LeadId);
        }

        await _mesaPublisher.PublishLeadConvertedAsync(new LeadConvertedIntegrationEvent(
            LeadId: e.LeadId,
            CrmContactId: e.CrmContactId,
            FullName: lead?.FullName ?? "Bilinmeyen",
            Email: lead?.Email,
            TenantId: tenantId,
            OccurredAt: e.OccurredAt
        ), cancellationToken).ConfigureAwait(false);
    }
}
