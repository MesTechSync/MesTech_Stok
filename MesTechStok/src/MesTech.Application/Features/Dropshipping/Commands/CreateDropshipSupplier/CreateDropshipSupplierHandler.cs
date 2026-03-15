using MediatR;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands.CreateDropshipSupplier;

public class CreateDropshipSupplierHandler : IRequestHandler<CreateDropshipSupplierCommand, Guid>
{
    private readonly IDropshipSupplierRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateDropshipSupplierHandler(IDropshipSupplierRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateDropshipSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = DropshipSupplier.Create(
            request.TenantId,
            request.Name,
            request.WebsiteUrl,
            request.MarkupType,
            request.MarkupValue);

        await _repository.AddAsync(supplier, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return supplier.Id;
    }
}
