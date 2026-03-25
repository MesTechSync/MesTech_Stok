using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Stores.Queries.GetStoreCredential;

public sealed class GetStoreCredentialHandler : IRequestHandler<GetStoreCredentialQuery, StoreCredentialDto?>
{
    private readonly IStoreRepository _storeRepository;
    private readonly IStoreCredentialRepository _credentialRepository;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly ILogger<GetStoreCredentialHandler> _logger;

    public GetStoreCredentialHandler(
        IStoreRepository storeRepository,
        IStoreCredentialRepository credentialRepository,
        ICredentialEncryptionService encryptionService,
        ILogger<GetStoreCredentialHandler> logger)
    {
        _storeRepository = storeRepository;
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<StoreCredentialDto?> Handle(
        GetStoreCredentialQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var store = await _storeRepository.GetByIdAsync(request.StoreId, cancellationToken);
        if (store is null)
            return null;

        var credentials = await _credentialRepository.GetByStoreIdAsync(request.StoreId, cancellationToken);
        if (credentials.Count == 0)
            return null;

        var maskedFields = new Dictionary<string, string>(StringComparer.Ordinal);
        string credentialType = string.Empty;

        foreach (var cred in credentials)
        {
            // Key format is "credentialType:fieldName"
            string fieldName;
            if (cred.Key.Contains(':', StringComparison.Ordinal))
            {
                var parts = cred.Key.Split(':', 2);
                credentialType = parts[0];
                fieldName = parts[1];
            }
            else
            {
                fieldName = cred.Key;
            }

            // Decrypt then mask — plaintext NEVER leaves the handler
            var plainText = _encryptionService.Decrypt(cred.EncryptedValue);
            maskedFields[fieldName] = _encryptionService.Mask(plainText);
        }

        var lastUpdated = credentials.Max(c => c.UpdatedAt);

        return new StoreCredentialDto
        {
            StoreId = request.StoreId,
            Platform = store.PlatformType.ToString(),
            CredentialType = credentialType,
            MaskedFields = maskedFields,
            LastUpdated = lastUpdated
        };
    }
}
