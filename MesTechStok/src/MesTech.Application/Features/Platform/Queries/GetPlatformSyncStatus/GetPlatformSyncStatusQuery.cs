using MediatR;
using MesTech.Application.Behaviors;
using MesTech.Application.DTOs.Platform;

namespace MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;

public record GetPlatformSyncStatusQuery(Guid TenantId)
    : IRequest<List<PlatformSyncStatusDto>>, ICacheableQuery
{
    public string CacheKey => $"PlatformSyncStatus_{TenantId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(1);
}
