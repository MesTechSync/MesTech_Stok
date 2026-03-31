using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.DeleteSupplier;

public sealed class DeleteSupplierHandler : IRequestHandler<DeleteSupplierCommand, bool>
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSupplierHandler(ISupplierRepository supplierRepository, IUnitOfWork unitOfWork)
    {
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<bool> Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId)
            .ConfigureAwait(false);

        if (supplier is null)
            return false;

        await _supplierRepository.DeleteAsync(request.SupplierId).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }
}
