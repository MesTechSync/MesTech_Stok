using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Calendar.Queries.GetCalendarEventById;

public record GetCalendarEventByIdQuery(Guid Id) : IRequest<CalendarEventDto?>;
