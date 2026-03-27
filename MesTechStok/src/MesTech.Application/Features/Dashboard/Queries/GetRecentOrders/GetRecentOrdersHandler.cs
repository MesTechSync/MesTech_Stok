using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dashboard.Queries.GetRecentOrders;

public sealed class GetRecentOrdersHandler
    : IRequestHandler<GetRecentOrdersQuery, IReadOnlyList<RecentOrderDto>>
{
    private readonly IOrderRepository _orderRepo;

    public GetRecentOrdersHandler(IOrderRepository orderRepo)
        => _orderRepo = orderRepo ?? throw new ArgumentNullException(nameof(orderRepo));

    public async Task<IReadOnlyList<RecentOrderDto>> Handle(
        GetRecentOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepo.GetByDateRangeAsync(
            request.TenantId,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(1),
            cancellationToken).ConfigureAwait(false);

        return orders
            .OrderByDescending(o => o.OrderDate)
            .Take(request.Count)
            .Select(o => new RecentOrderDto
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.CustomerName,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
                Platform = o.SourcePlatform?.ToString(),
                OrderDate = o.OrderDate
            })
            .ToList();
    }
}
