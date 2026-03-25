using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Stores.Commands.SaveStoreCredential;

public sealed class SaveStoreCredentialHandler : IRequestHandler<SaveStoreCredentialCommand, Guid>
{
    private readonly IStoreRepository _storeRepository;
    private readonly IStoreCredentialRepository _credentialRepository;
    private readonly IUnitOfWork _uow;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly ILogger<SaveStoreCredentialHandler> _logger;

    public SaveStoreCredentialHandler(
        IStoreRepository storeRepository,
        IStoreCredentialRepository credentialRepository,
        IUnitOfWork uow,
        ICredentialEncryptionService encryptionService,
        ILogger<SaveStoreCredentialHandler> logger)
    {
        _storeRepository = storeRepository;
        _credentialRepository = credentialRepository;
        _uow = uow;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<Guid> Handle(SaveStoreCredentialCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Verify store exists and belongs to the tenant
        var store = await _storeRepository.GetByIdAsync(request.StoreId, cancellationToken);
        if (store is null)
            throw new InvalidOperationException($"Store {request.StoreId} not found.");
        if (store.TenantId != request.TenantId)
            throw new UnauthorizedAccessException($"Store {request.StoreId} does not belong to tenant {request.TenantId}.");

        // Delete existing credentials for this store (upsert semantics)
        var existing = await _credentialRepository.GetByStoreIdAsync(request.StoreId, cancellationToken);
        foreach (var cred in existing)
        {
            cred.IsDeleted = true;
            cred.DeletedAt = DateTime.UtcNow;
            cred.DeletedBy = "system";
            await _credentialRepository.UpdateAsync(cred, cancellationToken);
        }

        // Save new encrypted credentials
        Guid? firstId = null;
        foreach (var field in request.Fields)
        {
            var credential = new StoreCredential
            {
                TenantId = request.TenantId,
                StoreId = request.StoreId,
                Key = $"{request.CredentialType}:{field.Key}",
                EncryptedValue = _encryptionService.Encrypt(field.Value),
                CreatedBy = "system",
                UpdatedBy = "system"
            };

            await _credentialRepository.AddAsync(credential, cancellationToken);

            firstId ??= credential.Id;
        }

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Saved {FieldCount} credential fields for Store {StoreId} (Platform: {Platform}, Type: {Type})",
            request.Fields.Count, request.StoreId, request.Platform, request.CredentialType);

        return firstId ?? throw new InvalidOperationException("No credential fields were provided.");
    }
}
