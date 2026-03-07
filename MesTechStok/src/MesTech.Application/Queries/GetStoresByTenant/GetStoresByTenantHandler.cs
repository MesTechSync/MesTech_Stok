using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetStoresByTenant;

public class GetStoresByTenantHandler : IRequestHandler<GetStoresByTenantQuery, IReadOnlyList<StoreDto>>
{
    private readonly IStoreRepository _storeRepository;

    public GetStoresByTenantHandler(IStoreRepository storeRepository)
    {
        _storeRepository = storeRepository;
    }

    public async Task<IReadOnlyList<StoreDto>> Handle(GetStoresByTenantQuery request, CancellationToken cancellationToken)
    {
        var stores = await _storeRepository.GetByTenantIdAsync(request.TenantId, cancellationToken);

        return stores.Select(s => new StoreDto
        {
            Id = s.Id,
            TenantId = s.TenantId,
            PlatformType = s.PlatformType,
            StoreName = s.StoreName,
            ExternalStoreId = s.ExternalStoreId,
            IsActive = s.IsActive,
            ProductMappingCount = s.ProductMappings?.Count ?? 0,
            CreatedAt = s.CreatedAt,
        }).ToList();
    }
}
