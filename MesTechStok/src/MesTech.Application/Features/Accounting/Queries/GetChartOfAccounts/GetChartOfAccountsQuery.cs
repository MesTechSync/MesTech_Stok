using MediatR;
using MesTech.Application.Behaviors;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetChartOfAccounts;

public record GetChartOfAccountsQuery(Guid TenantId, bool? IsActive = true)
    : IRequest<IReadOnlyList<ChartOfAccountsDto>>, ICacheableQuery
{
    public string CacheKey => $"ChartOfAccounts_{TenantId}_{IsActive}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
}
