using MediatR;
using MesTech.Application.Behaviors;
using MesTech.Application.DTOs.Finance;

namespace MesTech.Application.Features.Finance.Queries.GetCashFlow;

public record GetCashFlowQuery(Guid TenantId, int Year, int Month)
    : IRequest<CashFlowDto>, ICacheableQuery
{
    public string CacheKey => $"CashFlow_{TenantId}_{Year}_{Month}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
