using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetSuppliers;

public sealed class GetSuppliersHandler : IRequestHandler<GetSuppliersQuery, IReadOnlyList<SupplierListDto>>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetSuppliersHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<IReadOnlyList<SupplierListDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var suppliers = request.ActiveOnly
            ? await _supplierRepository.GetActiveAsync()
            : await _supplierRepository.GetAllAsync().ConfigureAwait(false);

        return suppliers.Select(s => new SupplierListDto
        {
            Id = s.Id,
            Name = s.Name,
            Code = s.Code,
            ContactPerson = s.ContactPerson,
            Email = s.Email,
            Phone = s.Phone,
            City = s.City,
            IsActive = s.IsActive,
            IsPreferred = s.IsPreferred,
            CurrentBalance = s.CurrentBalance,
        }).ToList();
    }
}
