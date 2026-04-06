using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Platform.Commands.DeleteStore;

public sealed class DeleteStoreHandler : IRequestHandler<DeleteStoreCommand, DeleteStoreResult>
{
    private readonly IStoreRepository _storeRepository;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DeleteStoreHandler> _logger;

    public DeleteStoreHandler(
        IStoreRepository storeRepository,
        IUnitOfWork uow,
        ILogger<DeleteStoreHandler> logger)
    {
        _storeRepository = storeRepository;
        _uow = uow;
        _logger = logger;
    }

    public async Task<DeleteStoreResult> Handle(
        DeleteStoreCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var store = await _storeRepository.GetByIdAsync(request.StoreId, cancellationToken)
            .ConfigureAwait(false);

        if (store is null)
        {
            return new DeleteStoreResult
            {
                IsSuccess = false,
                ErrorMessage = $"Store not found: {request.StoreId}"
            };
        }

        if (store.TenantId != request.TenantId)
        {
            _logger.LogWarning(
                "Tenant mismatch on DeleteStore: Store {StoreId} belongs to {OwnerTenant}, request from {RequestTenant}",
                request.StoreId, store.TenantId, request.TenantId);
            return new DeleteStoreResult
            {
                IsSuccess = false,
                ErrorMessage = "Store does not belong to this tenant."
            };
        }

        if (store.IsDeleted)
        {
            return new DeleteStoreResult
            {
                IsSuccess = false,
                ErrorMessage = "Store is already deleted."
            };
        }

        // Soft-delete: set flags on BaseEntity
        store.IsDeleted = true;
        store.DeletedAt = DateTime.UtcNow;
        store.DeletedBy = MesTech.Domain.Constants.DomainConstants.SystemUserName;
        store.IsActive = false;

        await _storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Soft-deleted Store {StoreId} ({StoreName}) for tenant {TenantId}",
            store.Id, store.StoreName, store.TenantId);

        return new DeleteStoreResult { IsSuccess = true };
    }
}
