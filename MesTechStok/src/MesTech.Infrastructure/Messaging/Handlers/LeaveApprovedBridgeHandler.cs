using MediatR;
using MesTech.Domain.Events.Hr;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// Izin onaylandi → MESA Bot calısana WhatsApp bildirimi gonderir.
/// DomainEventNotification wrapper kullanir — Domain katmani INotification bilmez.
/// </summary>
public sealed class LeaveApprovedBridgeHandler : INotificationHandler<DomainEventNotification<LeaveApprovedEvent>>
{
    private readonly IMesaEventPublisher _mesa;
    private readonly ILogger<LeaveApprovedBridgeHandler> _logger;

    public LeaveApprovedBridgeHandler(IMesaEventPublisher mesa,
        ILogger<LeaveApprovedBridgeHandler> logger)
    {
        _mesa = mesa;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<LeaveApprovedEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;

        _logger.LogInformation(
            "LeaveApproved bridge: EmployeeId={EmployeeId} LeaveId={LeaveId}",
            e.EmployeeId, e.LeaveId);

        // MESA Bot'a bildir — WhatsApp bildirimi tetikler
        await _mesa.PublishLeaveApprovedAsync(
            e.LeaveId, e.EmployeeId, e.OccurredAt, ct).ConfigureAwait(false);
    }
}
