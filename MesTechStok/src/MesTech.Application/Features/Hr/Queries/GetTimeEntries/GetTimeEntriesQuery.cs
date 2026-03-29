using MediatR;

namespace MesTech.Application.Features.Hr.Queries.GetTimeEntries;

public record GetTimeEntriesQuery(
    Guid TenantId,
    DateTime From,
    DateTime To,
    Guid? UserId = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<IReadOnlyList<TimeEntryDto>>;

public sealed class TimeEntryDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid WorkTaskId { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public int Minutes { get; init; }
    public string? Description { get; init; }
    public bool IsBillable { get; init; }
    public decimal? HourlyRate { get; init; }
}
