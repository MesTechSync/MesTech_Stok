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

    public async Task<Guid> Handle(CreateCalendarEventCommand req, CancellationToken ct)
    {
        var ev = CalendarEvent.Create(req.TenantId, req.Title, req.StartAt, req.EndAt,
            req.Type, req.IsAllDay, req.CreatedByUserId, req.Description, req.Location,
            null, req.RelatedOrderId, req.RelatedDealId, req.RelatedWorkTaskId);

        foreach (var userId in req.AttendeeUserIds ?? [])
            ev.AddAttendee(userId);

        await _repository.AddAsync(ev, ct);
        await _uow.SaveChangesAsync(ct);
        return ev.Id;
    }
}
