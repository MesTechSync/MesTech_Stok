using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetPlatformHealth;

/// <summary>
/// Platform saglik durumu sorgusu — her platform icin son sync ve hata sayisi.
/// Cache: 1 dakika (sık yenilenen metrik).
/// </summary>
public record GetPlatformHealthQuery(Guid TenantId)
    : IRequest<IReadOnlyList<PlatformHealthDto>>, ICacheableQuery
{
    public string CacheKey => $"PlatformHealth_{TenantId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(1);
}

/// <summary>
/// Platform saglik DTO.
/// </summary>
public record PlatformHealthDto
{
    public string Platform { get; init; } = string.Empty;
    public DateTime? LastSyncAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ErrorCount24h { get; init; }
}
