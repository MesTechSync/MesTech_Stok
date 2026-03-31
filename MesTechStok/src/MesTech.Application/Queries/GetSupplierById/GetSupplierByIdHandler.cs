using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetSupplierById;

public sealed class GetSupplierByIdHandler : IRequestHandler<GetSupplierByIdQuery, Supplier?>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetSupplierByIdHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
    }

    public async Task<Supplier?> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _supplierRepository.GetByIdAsync(request.SupplierId)
            .ConfigureAwait(false);
    }
}
