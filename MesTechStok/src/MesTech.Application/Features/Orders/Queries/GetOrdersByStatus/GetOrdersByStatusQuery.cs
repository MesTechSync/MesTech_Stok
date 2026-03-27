using MesTech.Application.Behaviors;
using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.Orders.Queries.GetOrdersByStatus;

public record GetOrdersByStatusQuery(Guid TenantId) : IRequest<OrderKanbanResult>, ICacheableQuery
{
    public string CacheKey => $"OrderKanban_{TenantId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(1);
}

public sealed class OrderKanbanResult
{
    public IReadOnlyList<KanbanColumnDto> Columns { get; init; } = [];
    public int TotalOrders { get; init; }
}

public sealed class KanbanColumnDto
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal TotalAmount { get; init; }
    public IReadOnlyList<KanbanOrderDto> Orders { get; init; } = [];
}

public sealed class KanbanOrderDto
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public decimal TotalAmount { get; init; }
    public string? Platform { get; init; }
    public DateTime OrderDate { get; init; }
}
