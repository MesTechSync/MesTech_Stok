using MediatR;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands.PlaceDropshipOrder;

public class PlaceDropshipOrderHandler : IRequestHandler<PlaceDropshipOrderCommand, Guid>
{
    private readonly IDropshipOrderRepository _repository;
    private readonly IUnitOfWork _uow;

    public PlaceDropshipOrderHandler(IDropshipOrderRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(PlaceDropshipOrderCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var order = DropshipOrder.Create(
            request.TenantId,
            request.OrderId,
            request.SupplierId,
            request.ProductId);

        order.PlaceWithSupplier(request.SupplierOrderRef);

        await _repository.AddAsync(order, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return order.Id;
    }
}
