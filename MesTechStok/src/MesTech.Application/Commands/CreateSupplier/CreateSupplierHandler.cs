using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateSupplier;

public sealed class CreateSupplierHandler : IRequestHandler<CreateSupplierCommand, SupplierCommandResult>
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSupplierHandler(ISupplierRepository supplierRepository, IUnitOfWork unitOfWork)
    {
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SupplierCommandResult> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var supplier = new Supplier
        {
            Name = request.Name,
            Code = request.Code,
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            City = request.City,
            TaxNumber = request.TaxNumber,
            TaxOffice = request.TaxOffice,
            PaymentTermDays = request.PaymentTermDays,
            IsActive = request.IsActive,
        };

        await _supplierRepository.AddAsync(supplier).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SupplierCommandResult
        {
            IsSuccess = true,
            SupplierId = supplier.Id,
        };
    }
}
