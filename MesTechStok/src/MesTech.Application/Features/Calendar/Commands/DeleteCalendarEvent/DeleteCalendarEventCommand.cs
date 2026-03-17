using MediatR;

namespace MesTech.Application.Features.Calendar.Commands.DeleteCalendarEvent;

public record DeleteCalendarEventCommand(Guid Id) : IRequest;
