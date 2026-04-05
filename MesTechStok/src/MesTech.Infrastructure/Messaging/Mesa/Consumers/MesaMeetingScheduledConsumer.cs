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
public sealed class MesaMeetingScheduledConsumer : IConsumer<MesaMeetingScheduledEvent>
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
            "Processing {Consumer} MessageId={MessageId} — '{Title}' at {StartAt}",
            nameof(MesaMeetingScheduledConsumer), context.MessageId, msg.Title, msg.StartAt);

        try
        {
            var eventId = await _mediator.Send(new CreateCalendarEventCommand(
                TenantId: msg.TenantId,
                Title: $"[MESA Bot] {msg.Title}",
                StartAt: msg.StartAt,
                EndAt: msg.EndAt,
                Type: EventType.Meeting,
                Location: msg.Location,
                AttendeeUserIds: msg.AttendeeUserIds,
                RelatedDealId: msg.RelatedDealId
            )).ConfigureAwait(false);

            _logger.LogInformation("CalendarEvent oluşturuldu: {EventId} for MessageId={MessageId}",
                eventId, context.MessageId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Consumer {Consumer} failed for MessageId={MessageId}",
                nameof(MesaMeetingScheduledConsumer), context.MessageId);
            throw; // MassTransit retry'a bırak
        }
    }
}
