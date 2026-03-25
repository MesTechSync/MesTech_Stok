using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Calendar.Commands.UpdateCalendarEvent;

public sealed class UpdateCalendarEventHandler : IRequestHandler<UpdateCalendarEventCommand>
{
    private readonly ICalendarEventRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdateCalendarEventHandler(ICalendarEventRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task Handle(UpdateCalendarEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"CalendarEvent {request.Id} not found.");

        if (request.IsCompleted.HasValue)
        {
            if (request.IsCompleted.Value)
                ev.MarkAsCompleted();
            else
                ev.MarkAsIncomplete();
        }

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
