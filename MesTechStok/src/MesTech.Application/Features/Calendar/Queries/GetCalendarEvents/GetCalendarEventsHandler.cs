using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Calendar.Queries.GetCalendarEvents;

public class GetCalendarEventsHandler : IRequestHandler<GetCalendarEventsQuery, IReadOnlyList<CalendarEventDto>>
{
    private readonly ICalendarEventRepository _repository;

    public GetCalendarEventsHandler(ICalendarEventRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<CalendarEventDto>> Handle(GetCalendarEventsQuery request, CancellationToken cancellationToken)
    {
        var from = request.From ?? DateTime.UtcNow.AddMonths(-1);
        var to = request.To ?? DateTime.UtcNow.AddMonths(3);

        var events = await _repository.GetByDateRangeAsync(request.TenantId, from, to, cancellationToken);
        return events.Adapt<List<CalendarEventDto>>().AsReadOnly();
    }
}
