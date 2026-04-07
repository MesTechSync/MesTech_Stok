using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Product.Commands.CreateProductSpecification;

public sealed class CreateProductSpecificationHandler : IRequestHandler<CreateProductSpecificationCommand, Guid>
{
    private readonly IProductSpecificationRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateProductSpecificationHandler(IProductSpecificationRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateProductSpecificationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = ProductSpecification.Create(
            request.TenantId,
            request.ProductId,
            request.SpecGroup,
            request.SpecName,
            request.SpecValue,
            request.Unit,
            request.DisplayOrder);

        await _repository.AddAsync(spec, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return spec.Id;
    }
}
