using MediatR;
using MesTech.Application.Behaviors;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetCommissionSummary;

public record GetCommissionSummaryQuery(Guid TenantId, DateTime From, DateTime To)
    : IRequest<CommissionSummaryDto>, ICacheableQuery
{
    public string CacheKey => $"CommissionSummary_{TenantId}_{From:yyyyMMdd}_{To:yyyyMMdd}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
