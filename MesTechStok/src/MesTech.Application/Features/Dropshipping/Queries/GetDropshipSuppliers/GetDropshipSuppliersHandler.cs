using MediatR;
using MesTech.Application.DTOs.Dropshipping;
using MesTech.Application.Interfaces.Dropshipping;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipSuppliers;

public class GetDropshipSuppliersHandler : IRequestHandler<GetDropshipSuppliersQuery, IReadOnlyList<DropshipSupplierDto>>
{
    private readonly IDropshipSupplierRepository _repository;

    public GetDropshipSuppliersHandler(IDropshipSupplierRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<DropshipSupplierDto>> Handle(GetDropshipSuppliersQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetByTenantAsync(request.TenantId, cancellationToken);
        return items.Select(s => new DropshipSupplierDto
        {
            Id = s.Id,
            Name = s.Name,
            WebsiteUrl = s.WebsiteUrl,
            ApiEndpoint = s.ApiEndpoint,
            MarkupType = s.MarkupType.ToString(),
            MarkupValue = s.MarkupValue,
            AutoSync = s.AutoSync,
            SyncIntervalMinutes = s.SyncIntervalMinutes,
            LastSyncAt = s.LastSyncAt,
            IsActive = s.IsActive
        }).ToList().AsReadOnly();
    }
}
