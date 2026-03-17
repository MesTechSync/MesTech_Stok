using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Calendar.Queries.GetCalendarEvents;

public record GetCalendarEventsQuery(
    Guid TenantId,
    DateTime? From = null,
    DateTime? To = null
) : IRequest<IReadOnlyList<CalendarEventDto>>;
