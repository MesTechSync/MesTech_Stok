using Mapster;
using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.ListOrders;

public class ListOrdersHandler : IRequestHandler<ListOrdersQuery, IReadOnlyList<OrderListDto>>
{
    private readonly IOrderRepository _orderRepository;

    public ListOrdersHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IReadOnlyList<OrderListDto>> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        var from = request.From ?? DateTime.UtcNow.AddDays(-30);
        var to = request.To ?? DateTime.UtcNow;

        var orders = await _orderRepository.GetByDateRangeAsync(from, to);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            orders = orders
                .Where(o => o.Status.ToString().Equals(request.Status, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }

        return orders.Adapt<List<OrderListDto>>().AsReadOnly();
    }
}
