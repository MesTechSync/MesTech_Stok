using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList;

public sealed class GetCargoTrackingListHandler : IRequestHandler<GetCargoTrackingListQuery, IReadOnlyList<CargoTrackingItemDto>>
{
    private readonly IOrderRepository _orderRepo;

    public GetCargoTrackingListHandler(IOrderRepository orderRepo) => _orderRepo = orderRepo;

    public async Task<IReadOnlyList<CargoTrackingItemDto>> Handle(GetCargoTrackingListQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepo.GetRecentAsync(request.TenantId, request.Count, cancellationToken);

        return orders
            .Where(o => o.ShippedAt.HasValue || o.TrackingNumber != null)
            .Select(o => new CargoTrackingItemDto
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                TrackingNumber = o.TrackingNumber,
                CargoProvider = o.CargoProvider?.ToString(),
                CargoBarcode = o.CargoBarcode,
                ShippedAt = o.ShippedAt,
                DeliveredAt = o.DeliveredAt,
                Status = o.DeliveredAt.HasValue ? "Delivered"
                    : o.ShippedAt.HasValue ? "Shipped"
                    : "Pending"
            })
            .ToList().AsReadOnly();
    }
}
