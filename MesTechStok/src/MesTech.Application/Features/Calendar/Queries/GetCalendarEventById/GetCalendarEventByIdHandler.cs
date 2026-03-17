using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Calendar.Queries.GetCalendarEventById;

public class GetCalendarEventByIdHandler : IRequestHandler<GetCalendarEventByIdQuery, CalendarEventDto?>
{
    private readonly ICalendarEventRepository _repository;

    public GetCalendarEventByIdHandler(ICalendarEventRepository repository)
        => _repository = repository;

    public async Task<CalendarEventDto?> Handle(GetCalendarEventByIdQuery request, CancellationToken cancellationToken)
    {
        var ev = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return ev?.Adapt<CalendarEventDto>();
    }
}
