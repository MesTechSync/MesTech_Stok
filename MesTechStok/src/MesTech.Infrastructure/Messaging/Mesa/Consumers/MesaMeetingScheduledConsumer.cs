using MassTransit;
using MediatR;
using MesTech.Application.Features.Calendar.Commands.CreateCalendarEvent;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Consumers;

/// <summary>
/// MESA Bot üzerinden WhatsApp/Telegram ile ayarlanan toplantıları
/// MesTech Takvim'e otomatik ekler.
/// </summary>
public class MesaMeetingScheduledConsumer : IConsumer<MesaMeetingScheduledEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<MesaMeetingScheduledConsumer> _logger;

    public MesaMeetingScheduledConsumer(IMediator mediator, ILogger<MesaMeetingScheduledConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MesaMeetingScheduledEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "MESA→MesTech: Toplantı alındı '{Title}' — {StartAt}",
            msg.Title, msg.StartAt);

        var eventId = await _mediator.Send(new CreateCalendarEventCommand(
            TenantId: msg.TenantId,
            Title: $"[MESA Bot] {msg.Title}",
            StartAt: msg.StartAt,
            EndAt: msg.EndAt,
            Type: EventType.Meeting,
            Location: msg.Location,
            AttendeeUserIds: msg.AttendeeUserIds,
            RelatedDealId: msg.RelatedDealId
        ));

        _logger.LogInformation("CalendarEvent oluşturuldu: {EventId}", eventId);
    }
}
