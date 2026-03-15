using MediatR;
using MesTech.Domain.Entities.Calendar;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Calendar.Commands.CreateCalendarEvent;

public class CreateCalendarEventHandler : IRequestHandler<CreateCalendarEventCommand, Guid>
{
    private readonly ICalendarEventRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateCalendarEventHandler(ICalendarEventRepository repo, IUnitOfWork uow)
        => (_repository, _uow) = (repo, uow);

    public async Task<Guid> Handle(CreateCalendarEventCommand request, CancellationToken cancellationToken)
    {
        var ev = CalendarEvent.Create(request.TenantId, request.Title, request.StartAt, request.EndAt,
            request.Type, request.IsAllDay, request.CreatedByUserId, request.Description, request.Location,
            null, request.RelatedOrderId, request.RelatedDealId, request.RelatedWorkTaskId);

        foreach (var userId in request.AttendeeUserIds ?? [])
            ev.AddAttendee(userId);

        await _repository.AddAsync(ev, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return ev.Id;
    }
}
