using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList;

public sealed class GetCargoTrackingListHandler : IRequestHandler<GetCargoTrackingListQuery, IReadOnlyList<CargoTrackingItemDto>>
{
    private readonly IOrderRepository _orderRepo;
    private readonly ILogger<GetCargoTrackingListHandler> _logger;

    public GetCargoTrackingListHandler(IOrderRepository orderRepo, ILogger<GetCargoTrackingListHandler> logger)
    {
        _orderRepo = orderRepo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CargoTrackingItemDto>> Handle(GetCargoTrackingListQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Entities.Order> orders;
        try
        {
            orders = await _orderRepo.GetRecentAsync(request.TenantId, request.Count, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DB unavailable for CargoTrackingList — returning empty list");
            return Array.Empty<CargoTrackingItemDto>();
        }

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
