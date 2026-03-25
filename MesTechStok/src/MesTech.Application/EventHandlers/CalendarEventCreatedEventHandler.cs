using MesTech.Domain.Events.Calendar;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Takvim etkinliği oluşturulduğunda loglama yapar.
/// Gelecekte: hatırlatıcı zamanlama tetiklenecek.
/// </summary>
public interface ICalendarEventCreatedEventHandler
{
    Task HandleAsync(CalendarEventCreatedEvent domainEvent, CancellationToken ct);
}

public sealed class CalendarEventCreatedEventHandler : ICalendarEventCreatedEventHandler
{
    private readonly ILogger<CalendarEventCreatedEventHandler> _logger;

    public CalendarEventCreatedEventHandler(ILogger<CalendarEventCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(CalendarEventCreatedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "CalendarEventCreated — EventId={EventId}, StartAt={StartAt}, OccurredAt={OccurredAt}",
            domainEvent.EventId, domainEvent.StartAt, domainEvent.OccurredAt);

        // FUTURE: Trigger reminder scheduling

        return Task.CompletedTask;
    }
}
