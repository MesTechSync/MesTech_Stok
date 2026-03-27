using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Stock.Queries.GetStockSummary;

public record GetStockSummaryQuery(Guid TenantId) : IRequest<StockSummaryResult>, ICacheableQuery
{
    public string CacheKey => $"StockSummary_{TenantId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}

public sealed class StockSummaryResult
{
    public int TotalProducts { get; init; }
    public int InStockProducts { get; init; }
    public int OutOfStockProducts { get; init; }
    public int LowStockProducts { get; init; }
    public decimal TotalStockValue { get; init; }
    public int TotalUnits { get; init; }
}
