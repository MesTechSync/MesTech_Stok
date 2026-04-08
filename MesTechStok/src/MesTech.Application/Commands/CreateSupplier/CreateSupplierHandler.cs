using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateSupplier;

public sealed class CreateSupplierHandler : IRequestHandler<CreateSupplierCommand, SupplierCommandResult>
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public CreateSupplierHandler(
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
    }

    public async Task<SupplierCommandResult> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId = _tenantProvider.GetCurrentTenantId();

        var supplier = Supplier.Create(
            tenantId,
            request.Name,
            request.Code,
            request.Email,
            request.Phone);

        supplier.ContactPerson = request.ContactPerson;
        supplier.Address = request.Address;
        supplier.City = request.City;
        supplier.TaxNumber = request.TaxNumber;
        supplier.TaxOffice = request.TaxOffice;
        supplier.PaymentTermDays = request.PaymentTermDays;
        supplier.IsActive = request.IsActive;

        await _supplierRepository.AddAsync(supplier, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SupplierCommandResult
        {
            IsSuccess = true,
            SupplierId = supplier.Id,
        };
    }
}
