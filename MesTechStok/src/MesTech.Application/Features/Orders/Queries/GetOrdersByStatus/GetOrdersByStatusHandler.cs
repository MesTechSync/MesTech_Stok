using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Orders.Queries.GetOrdersByStatus;

public sealed class GetOrdersByStatusHandler : IRequestHandler<GetOrdersByStatusQuery, OrderKanbanResult>
{
    private readonly IOrderRepository _orderRepo;

    public GetOrdersByStatusHandler(IOrderRepository orderRepo)
        => _orderRepo = orderRepo ?? throw new ArgumentNullException(nameof(orderRepo));

    public async Task<OrderKanbanResult> Handle(GetOrdersByStatusQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepo.GetByDateRangeAsync(
            request.TenantId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(1), cancellationToken)
            .ConfigureAwait(false);

        var columns = orders
            .GroupBy(o => o.Status.ToString())
            .Select(g => new KanbanColumnDto
            {
                Status = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(o => o.TotalAmount),
                Orders = g.OrderByDescending(o => o.OrderDate)
                    .Take(20)
                    .Select(o => new KanbanOrderDto
                    {
                        OrderId = o.Id,
                        OrderNumber = o.OrderNumber,
                        CustomerName = o.CustomerName,
                        TotalAmount = o.TotalAmount,
                        Platform = o.SourcePlatform?.ToString(),
                        OrderDate = o.OrderDate
                    }).ToList()
            }).ToList();

        return new OrderKanbanResult
        {
            Columns = columns,
            TotalOrders = orders.Count
        };
    }
}
