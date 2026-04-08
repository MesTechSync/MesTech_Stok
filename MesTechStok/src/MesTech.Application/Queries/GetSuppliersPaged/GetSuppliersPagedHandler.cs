using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetSuppliersPaged;

public sealed class GetSuppliersPagedHandler : IRequestHandler<GetSuppliersPagedQuery, PagedSupplierResult>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetSuppliersPagedHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
    }

    public async Task<PagedSupplierResult> Handle(GetSuppliersPagedQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var allSuppliers = await _supplierRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        var filtered = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? allSuppliers
            : allSuppliers.Where(s =>
                s.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                s.Code.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();

        var totalCount = filtered.Count;
        var items = filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SupplierItemDto
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
                CreatedDate = s.CreatedAt,
            })
            .ToList();

        return new PagedSupplierResult
        {
            Items = items,
            TotalCount = totalCount,
        };
    }
}
