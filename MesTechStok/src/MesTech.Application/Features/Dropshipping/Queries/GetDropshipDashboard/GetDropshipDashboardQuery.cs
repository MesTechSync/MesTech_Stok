using MediatR;
using MesTech.Application.Behaviors;
using MesTech.Application.DTOs.Platform;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipDashboard;

public record GetDropshipDashboardQuery(Guid TenantId)
    : IRequest<DropshipDashboardDto>, ICacheableQuery
{
    public string CacheKey => $"DropshipDashboard_{TenantId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}
