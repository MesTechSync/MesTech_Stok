using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Platform.Commands.UpdateStore;

public sealed class UpdateStoreHandler : IRequestHandler<UpdateStoreCommand, UpdateStoreResult>
{
    private readonly IStoreRepository _storeRepository;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UpdateStoreHandler> _logger;

    public UpdateStoreHandler(
        IStoreRepository storeRepository,
        IUnitOfWork uow,
        ILogger<UpdateStoreHandler> logger)
    {
        _storeRepository = storeRepository;
        _uow = uow;
        _logger = logger;
    }

    public async Task<UpdateStoreResult> Handle(
        UpdateStoreCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var store = await _storeRepository.GetByIdAsync(request.StoreId, cancellationToken)
            .ConfigureAwait(false);

        if (store is null)
        {
            return new UpdateStoreResult
            {
                IsSuccess = false,
                ErrorMessage = $"Store not found: {request.StoreId}"
            };
        }

        if (store.TenantId != request.TenantId)
        {
            _logger.LogWarning(
                "Tenant mismatch on UpdateStore: Store {StoreId} belongs to {OwnerTenant}, request from {RequestTenant}",
                request.StoreId, store.TenantId, request.TenantId);
            return new UpdateStoreResult
            {
                IsSuccess = false,
                ErrorMessage = "Store does not belong to this tenant."
            };
        }

        store.StoreName = request.StoreName.Trim();
        store.IsActive = request.IsActive;
        store.UpdatedAt = DateTime.UtcNow;
        store.UpdatedBy = MesTech.Domain.Constants.DomainConstants.SystemUserName;

        await _storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Updated Store {StoreId} ({StoreName}), IsActive={IsActive}",
            store.Id, store.StoreName, store.IsActive);

        return new UpdateStoreResult { IsSuccess = true };
    }
}
