using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Calendar.Commands.DeleteCalendarEvent;

public sealed class DeleteCalendarEventHandler : IRequestHandler<DeleteCalendarEventCommand>
{
    private readonly ICalendarEventRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteCalendarEventHandler(ICalendarEventRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task Handle(DeleteCalendarEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"CalendarEvent {request.Id} not found.");

        ev.IsDeleted = true;
        ev.DeletedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
