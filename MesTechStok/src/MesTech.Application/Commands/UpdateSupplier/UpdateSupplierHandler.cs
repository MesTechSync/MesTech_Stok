using MediatR;
using MesTech.Application.Commands.CreateSupplier;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.UpdateSupplier;

public sealed class UpdateSupplierHandler : IRequestHandler<UpdateSupplierCommand, SupplierCommandResult>
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSupplierHandler(ISupplierRepository supplierRepository, IUnitOfWork unitOfWork)
    {
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SupplierCommandResult> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var supplier = await _supplierRepository.GetByIdAsync(request.Id).ConfigureAwait(false);
        if (supplier == null)
            return new SupplierCommandResult { IsSuccess = false, ErrorMessage = $"Supplier {request.Id} not found." };

        supplier.Name = request.Name;
        supplier.Code = request.Code;
        supplier.ContactPerson = request.ContactPerson;
        supplier.Email = request.Email;
        supplier.Phone = request.Phone;
        supplier.Address = request.Address;
        supplier.City = request.City;
        supplier.TaxNumber = request.TaxNumber;
        supplier.TaxOffice = request.TaxOffice;
        supplier.PaymentTermDays = request.PaymentTermDays;
        supplier.IsActive = request.IsActive;

        await _supplierRepository.UpdateAsync(supplier).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SupplierCommandResult
        {
            IsSuccess = true,
            SupplierId = supplier.Id,
        };
    }
}
