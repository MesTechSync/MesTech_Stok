using MediatR;
using MesTech.Application.DTOs.Dropshipping;
using MesTech.Application.Interfaces.Dropshipping;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipOrders;

public class GetDropshipOrdersHandler : IRequestHandler<GetDropshipOrdersQuery, IReadOnlyList<DropshipOrderDto>>
{
    private readonly IDropshipOrderRepository _repository;

    public GetDropshipOrdersHandler(IDropshipOrderRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<DropshipOrderDto>> Handle(GetDropshipOrdersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var items = await _repository.GetByTenantAsync(request.TenantId, cancellationToken);
        return items.Select(o => new DropshipOrderDto
        {
            Id = o.Id,
            OrderId = o.OrderId,
            DropshipSupplierId = o.DropshipSupplierId,
            DropshipProductId = o.DropshipProductId,
            SupplierOrderRef = o.SupplierOrderRef,
            SupplierTrackingNumber = o.SupplierTrackingNumber,
            Status = o.Status.ToString(),
            FailureReason = o.FailureReason,
            OrderedAt = o.OrderedAt,
            ShippedAt = o.ShippedAt,
            DeliveredAt = o.DeliveredAt
        }).ToList().AsReadOnly();
    }
}
