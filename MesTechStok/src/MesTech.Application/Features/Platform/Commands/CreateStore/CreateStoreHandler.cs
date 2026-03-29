using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Platform.Commands.CreateStore;

public sealed class CreateStoreHandler : IRequestHandler<CreateStoreCommand, CreateStoreResult>
{
    private readonly IStoreRepository _storeRepository;
    private readonly IStoreCredentialRepository _credentialRepository;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly IAdapterFactory _adapterFactory;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateStoreHandler> _logger;

    public CreateStoreHandler(
        IStoreRepository storeRepository,
        IStoreCredentialRepository credentialRepository,
        ICredentialEncryptionService encryptionService,
        IAdapterFactory adapterFactory,
        IUnitOfWork uow,
        ILogger<CreateStoreHandler> logger)
    {
        _storeRepository = storeRepository;
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _adapterFactory = adapterFactory;
        _uow = uow;
        _logger = logger;
    }

#pragma warning disable MA0051 // Method is too long — store creation + credential save + connection test is one atomic flow
    public async Task<CreateStoreResult> Handle(
        CreateStoreCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.StoreName))
            return new CreateStoreResult { IsSuccess = false, ErrorMessage = "Store name is required." };

        // 1. Create Store entity
        var store = new Store
        {
            TenantId = request.TenantId,
            PlatformType = request.PlatformType,
            StoreName = request.StoreName.Trim(),
            IsActive = true,
            CreatedBy = MesTech.Domain.Constants.DomainConstants.SystemUserName,
            UpdatedBy = MesTech.Domain.Constants.DomainConstants.SystemUserName
        };

        await _storeRepository.AddAsync(store, cancellationToken);

        // 2. Save encrypted credentials
        var credentialDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in request.Credentials)
        {
            var credential = new StoreCredential
            {
                TenantId = request.TenantId,
                StoreId = store.Id,
                Key = kvp.Key,
                EncryptedValue = _encryptionService.Encrypt(kvp.Value),
                CreatedBy = MesTech.Domain.Constants.DomainConstants.SystemUserName,
                UpdatedBy = MesTech.Domain.Constants.DomainConstants.SystemUserName
            };
            await _credentialRepository.AddAsync(credential, cancellationToken);
            credentialDict[kvp.Key] = kvp.Value;
        }

        await _uow.SaveChangesAsync(cancellationToken);

        // 3. Test connection via adapter
        var adapter = _adapterFactory.Resolve(request.PlatformType);
        if (adapter is not null)
        {
            try
            {
                var testResult = await adapter.TestConnectionAsync(
                    credentialDict, cancellationToken);

                if (!testResult.IsSuccess)
                {
                    // Rollback: delete store and credentials
                    await _storeRepository.DeleteAsync(store, cancellationToken);
                    await _uow.SaveChangesAsync(cancellationToken);

                    _logger.LogWarning(
                        "Connection test failed for Store {StoreName} ({Platform}): {Error}",
                        request.StoreName, request.PlatformType, testResult.ErrorMessage);

                    return new CreateStoreResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Connection test failed: {testResult.ErrorMessage}"
                    };
                }
            }
#pragma warning disable CA1031 // Adapter.TestConnectionAsync can throw any exception type
            catch (Exception ex)
#pragma warning restore CA1031
            {
                // Rollback on exception
                await _storeRepository.DeleteAsync(store, cancellationToken);
                await _uow.SaveChangesAsync(cancellationToken);

                _logger.LogError(ex,
                    "Connection test threw for Store {StoreName} ({Platform})",
                    request.StoreName, request.PlatformType);

                return new CreateStoreResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Connection test error: {ex.Message}"
                };
            }
        }

        _logger.LogInformation(
            "Created Store {StoreId} ({StoreName}) for Platform {Platform}",
            store.Id, store.StoreName, request.PlatformType);

        return new CreateStoreResult { IsSuccess = true, StoreId = store.Id };
    }
}
