using System.Diagnostics;
using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Platform.Commands.TestStoreConnection;

public sealed class TestStoreConnectionHandler
    : IRequestHandler<TestStoreConnectionCommand, ConnectionTestResultDto>
{
    private readonly IStoreRepository _storeRepository;
    private readonly IStoreCredentialRepository _credentialRepository;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly IAdapterFactory _adapterFactory;
    private readonly ILogger<TestStoreConnectionHandler> _logger;

    public TestStoreConnectionHandler(
        IStoreRepository storeRepository,
        IStoreCredentialRepository credentialRepository,
        ICredentialEncryptionService encryptionService,
        IAdapterFactory adapterFactory,
        ILogger<TestStoreConnectionHandler> logger)
    {
        _storeRepository = storeRepository;
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _adapterFactory = adapterFactory;
        _logger = logger;
    }

    public async Task<ConnectionTestResultDto> Handle(
        TestStoreConnectionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sw = Stopwatch.StartNew();

        var store = await _storeRepository.GetByIdAsync(request.StoreId, cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return new ConnectionTestResultDto
            {
                IsSuccess = false,
                ErrorMessage = $"Store {request.StoreId} not found.",
                ResponseTime = sw.Elapsed
            };
        }

        // Load and decrypt credentials
        var credentials = await _credentialRepository
            .GetByStoreIdAsync(request.StoreId, cancellationToken);

        var credentialDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cred in credentials)
        {
            credentialDict[cred.Key] = _encryptionService.Decrypt(cred.EncryptedValue);
        }

        // Resolve adapter
        var adapter = _adapterFactory.Resolve(store.PlatformType);
        if (adapter is null)
        {
            return new ConnectionTestResultDto
            {
                IsSuccess = false,
                PlatformCode = store.PlatformType.ToString(),
                ErrorMessage = $"No adapter available for {store.PlatformType}.",
                ResponseTime = sw.Elapsed
            };
        }

        try
        {
            var result = await adapter.TestConnectionAsync(credentialDict, cancellationToken).ConfigureAwait(false);
            result.ResponseTime = sw.Elapsed;
            return result;
        }
#pragma warning disable CA1031 // Adapter.TestConnectionAsync can throw any exception type
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogError(ex,
                "TestConnection failed for Store {StoreId} ({Platform})",
                request.StoreId, store.PlatformType);

            return new ConnectionTestResultDto
            {
                IsSuccess = false,
                PlatformCode = store.PlatformType.ToString(),
                ErrorMessage = $"Connection error: {ex.Message}",
                ResponseTime = sw.Elapsed
            };
        }
    }
}
