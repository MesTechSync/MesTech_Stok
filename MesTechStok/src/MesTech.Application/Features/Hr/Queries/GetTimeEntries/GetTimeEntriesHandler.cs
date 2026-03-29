using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Hr.Queries.GetTimeEntries;

public sealed class GetTimeEntriesHandler
    : IRequestHandler<GetTimeEntriesQuery, IReadOnlyList<TimeEntryDto>>
{
    private readonly ITimeEntryRepository _repository;

    public GetTimeEntriesHandler(ITimeEntryRepository repository)
        => _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<IReadOnlyList<TimeEntryDto>> Handle(
        GetTimeEntriesQuery request, CancellationToken cancellationToken)
    {
        var entries = await _repository.GetByTenantAsync(
            request.TenantId, request.From, request.To,
            request.UserId, request.Page, request.PageSize,
            cancellationToken);

        return entries
            .Select(e => new TimeEntryDto
            {
                Id = e.Id,
                UserId = e.UserId,
                WorkTaskId = e.WorkTaskId,
                StartedAt = e.StartedAt,
                EndedAt = e.EndedAt,
                Minutes = e.Minutes,
                Description = e.Description,
                IsBillable = e.IsBillable,
                HourlyRate = e.HourlyRate
            })
            .ToList()
            .AsReadOnly();
    }
}
