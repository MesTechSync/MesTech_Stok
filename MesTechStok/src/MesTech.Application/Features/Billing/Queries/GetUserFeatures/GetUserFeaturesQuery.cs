using MesTech.Application.Behaviors;
using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.Billing.Queries.GetUserFeatures;

public record GetUserFeaturesQuery(Guid TenantId) : IRequest<UserFeaturesResult>, ICacheableQuery
{
    public string CacheKey => $"UserFeatures_{TenantId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public sealed class UserFeaturesResult
{
    public SubscriptionTier Tier { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public int MaxPlatforms { get; init; }
    public int MaxProducts { get; init; }
    public int MaxUsers { get; init; }
    public IReadOnlyList<string> EnabledFeatures { get; init; } = [];
    public IReadOnlyList<string> LockedFeatures { get; init; } = [];
    public int DaysRemaining { get; init; }
}
