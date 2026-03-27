using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetRecentOrders;

public record GetRecentOrdersQuery(Guid TenantId, int Count = 10)
    : IRequest<IReadOnlyList<RecentOrderDto>>, ICacheableQuery
{
    public string CacheKey => $"RecentOrders_{TenantId}_{Count}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(1);
}

public sealed class RecentOrderDto
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Platform { get; init; }
    public DateTime OrderDate { get; init; }
}
