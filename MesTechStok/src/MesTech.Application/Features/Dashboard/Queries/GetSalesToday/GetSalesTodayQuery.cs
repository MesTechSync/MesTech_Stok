using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetSalesToday;

/// <summary>
/// Bugunku satis ozeti sorgusu — bugun vs dun karsilastirmali.
/// Cache: 3 dakika.
/// </summary>
public record GetSalesTodayQuery(Guid TenantId)
    : IRequest<SalesTodayDto>, ICacheableQuery
{
    public string CacheKey => $"SalesToday_{TenantId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(3);
}

/// <summary>
/// Bugunku satis ozet DTO.
/// </summary>
public record SalesTodayDto
{
    public decimal Today { get; init; }
    public decimal Yesterday { get; init; }
    public decimal ChangePercent { get; init; }
}
