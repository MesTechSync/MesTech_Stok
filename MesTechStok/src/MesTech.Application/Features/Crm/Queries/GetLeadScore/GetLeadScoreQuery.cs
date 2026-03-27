using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Crm.Queries.GetLeadScore;

public record GetLeadScoreQuery(Guid LeadId, Guid TenantId)
    : IRequest<LeadScoreResult>, ICacheableQuery
{
    public string CacheKey => $"LeadScore_{LeadId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public sealed class LeadScoreResult
{
    public Guid LeadId { get; init; }
    public int Score { get; init; }
    public string ScoreLabel { get; init; } = "Unknown";
    public string? Reasoning { get; init; }
}
