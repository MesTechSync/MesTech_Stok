using MediatR;
using MesTech.Application.Behaviors;

namespace MesTech.Application.Queries.GetProductDbStatus;

public record GetProductDbStatusQuery() : IRequest<ProductDbStatusDto>, ICacheableQuery
{
    public string CacheKey => "ProductDbStatus_Global";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(1);
}

public sealed class ProductDbStatusDto
{
    public bool IsConnected { get; set; }
    public int ActiveCount { get; set; }
    public int TotalCount { get; set; }
}
