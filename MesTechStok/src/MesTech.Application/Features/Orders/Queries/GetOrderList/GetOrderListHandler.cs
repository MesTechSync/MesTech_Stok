using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Orders.Queries.GetOrderList;

public sealed class GetOrderListHandler : IRequestHandler<GetOrderListQuery, IReadOnlyList<OrderListItemDto>>
{
    private readonly IOrderRepository _orderRepo;

    public GetOrderListHandler(IOrderRepository orderRepo) => _orderRepo = orderRepo;

    public async Task<IReadOnlyList<OrderListItemDto>> Handle(GetOrderListQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepo.GetRecentAsync(request.TenantId, request.Count, cancellationToken);

        return orders.Select(o => new OrderListItemDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerName = o.CustomerName,
            Status = o.Status.ToString(),
            PaymentStatus = o.PaymentStatus,
            SourcePlatform = o.SourcePlatform?.ToString(),
            TotalAmount = o.TotalAmount,
            TotalItems = o.TotalItems,
            OrderDate = o.OrderDate,
            TrackingNumber = o.TrackingNumber
        }).ToList().AsReadOnly();
    }
}
