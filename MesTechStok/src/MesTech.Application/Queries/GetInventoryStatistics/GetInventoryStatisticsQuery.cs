using MediatR;
using MesTech.Application.Behaviors;
using MesTech.Application.DTOs;

namespace MesTech.Application.Queries.GetInventoryStatistics;

public record GetInventoryStatisticsQuery : IRequest<InventoryStatisticsDto>, ICacheableQuery
{
    public string CacheKey => "InventoryStatistics_Global";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}
