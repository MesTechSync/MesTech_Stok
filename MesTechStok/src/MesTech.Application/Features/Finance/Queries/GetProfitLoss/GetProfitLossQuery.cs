using MediatR;
using MesTech.Application.Behaviors;
using MesTech.Application.DTOs.Finance;

namespace MesTech.Application.Features.Finance.Queries.GetProfitLoss;

public record GetProfitLossQuery(Guid TenantId, int Year, int Month)
    : IRequest<ProfitLossDto>, ICacheableQuery
{
    public string CacheKey => $"ProfitLoss_{TenantId}_{Year}_{Month}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
