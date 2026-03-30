using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Stores.Queries.GetStoreDetail;

public sealed class GetStoreDetailHandler : IRequestHandler<GetStoreDetailQuery, StoreDetailDto?>
{
    private readonly IStoreRepository _storeRepository;
    private readonly IStoreCredentialRepository _credentialRepository;
    private readonly ILogger<GetStoreDetailHandler> _logger;

    public GetStoreDetailHandler(
        IStoreRepository storeRepository,
        IStoreCredentialRepository credentialRepository,
        ILogger<GetStoreDetailHandler> logger)
    {
        _storeRepository = storeRepository;
        _credentialRepository = credentialRepository;
        _logger = logger;
    }

    public async Task<StoreDetailDto?> Handle(
        GetStoreDetailQuery request, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.GetByIdAsync(request.StoreId, cancellationToken);
        if (store is null)
            return null;

        if (store.TenantId != request.TenantId)
        {
            _logger.LogWarning(
                "Store {StoreId} does not belong to tenant {TenantId}",
                request.StoreId, request.TenantId);
            return null;
        }

        var credentials = await _credentialRepository.GetByStoreIdAsync(request.StoreId, cancellationToken);
        var credentialStatus = credentials.Count > 0 ? "Configured" : "NotConfigured";

        var productCount = store.ProductMappings?.Count ?? 0;

        return new StoreDetailDto
        {
            StoreId = store.Id,
            Name = store.StoreName,
            Platform = store.PlatformType.ToString(),
            IsActive = store.IsActive,
            LastSyncAt = store.UpdatedAt != default ? store.UpdatedAt : null,
            ProductCount = productCount,
            CredentialStatus = credentialStatus,
            WebhookStatus = store.IsActive ? "Active" : "Inactive"
        };
    }
}
