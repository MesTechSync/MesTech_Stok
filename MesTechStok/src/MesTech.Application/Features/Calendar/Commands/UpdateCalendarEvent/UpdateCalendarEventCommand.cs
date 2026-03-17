using MediatR;

namespace MesTech.Application.Features.Calendar.Commands.UpdateCalendarEvent;

public record UpdateCalendarEventCommand(
    Guid Id,
    bool? IsCompleted = null
) : IRequest;
