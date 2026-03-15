using MediatR;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands.LinkDropshipProduct;

public class LinkDropshipProductHandler : IRequestHandler<LinkDropshipProductCommand, Unit>
{
    private readonly IDropshipProductRepository _repository;
    private readonly IUnitOfWork _uow;

    public LinkDropshipProductHandler(IDropshipProductRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Unit> Handle(LinkDropshipProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.DropshipProductId, cancellationToken)
            ?? throw new InvalidOperationException($"Dropship product not found: {request.DropshipProductId}");

        if (product.TenantId != request.TenantId)
            throw new InvalidOperationException("Product does not belong to the specified tenant.");

        product.LinkToProduct(request.MesTechProductId);

        await _repository.UpdateAsync(product, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
