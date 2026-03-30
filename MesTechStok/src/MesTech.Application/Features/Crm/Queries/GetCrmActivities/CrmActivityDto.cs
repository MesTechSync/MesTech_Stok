using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Crm.Queries.GetCrmActivities;

public sealed class CrmActivityDto
{
    public Guid Id { get; init; }
    public ActivityType Type { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ContactName { get; init; }
    public DateTime OccurredAt { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime CreatedAt { get; init; }
}
