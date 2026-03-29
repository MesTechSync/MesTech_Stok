using MediatR;
using MesTech.Application.Behaviors;
using MesTech.Application.DTOs.Crm;

namespace MesTech.Application.Features.Crm.Queries.GetCrmDashboard;

public record GetCrmDashboardQuery(Guid TenantId)
    : IRequest<CrmDashboardDto>, ICacheableQuery
{
    public string CacheKey => $"CrmDashboard_{TenantId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}
