#pragma warning disable MA0051 // Method is too long — credential test handler is a single cohesive operation
#pragma warning disable CA1031 // Catch general exception — test handler must return user-friendly error for any failure
using System.Diagnostics;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Stores.Commands.TestStoreCredential;

public sealed class TestStoreCredentialHandler : IRequestHandler<TestStoreCredentialCommand, CredentialTestResult>
{
    private readonly IStoreRepository _storeRepository;
    private readonly IStoreCredentialRepository _credentialRepository;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly IAdapterFactory _adapterFactory;
    private readonly ILogger<TestStoreCredentialHandler> _logger;

    private const int TimeoutMs = 15_000;

    public TestStoreCredentialHandler(
        IStoreRepository storeRepository,
        IStoreCredentialRepository credentialRepository,
        ICredentialEncryptionService encryptionService,
        IAdapterFactory adapterFactory,
        ILogger<TestStoreCredentialHandler> logger)
    {
        _storeRepository = storeRepository;
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _adapterFactory = adapterFactory;
        _logger = logger;
    }

    public async Task<CredentialTestResult> Handle(
        TestStoreCredentialCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var store = await _storeRepository.GetByIdAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return new CredentialTestResult
            {
                Success = false,
                Message = $"Store {request.StoreId} not found.",
                Platform = "unknown"
            };
        }

        var platformCode = store.PlatformType.ToString();
        var adapter = _adapterFactory.Resolve(platformCode);
        if (adapter is null)
        {
            return new CredentialTestResult
            {
                Success = false,
                Message = $"No adapter found for platform '{platformCode}'.",
                Platform = platformCode
            };
        }

        // Load and decrypt credentials
        var credentials = await _credentialRepository.GetByStoreIdAsync(request.StoreId, cancellationToken);
        if (credentials.Count == 0)
        {
            return new CredentialTestResult
            {
                Success = false,
                Message = "No credentials saved for this store.",
                Platform = platformCode
            };
        }

        var decryptedFields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var cred in credentials)
        {
            // Key format is "credentialType:fieldName"
            var fieldName = cred.Key.Contains(':', StringComparison.Ordinal)
                ? cred.Key[(cred.Key.IndexOf(':', StringComparison.Ordinal) + 1)..]
                : cred.Key;
            decryptedFields[fieldName] = _encryptionService.Decrypt(cred.EncryptedValue);
        }

        // Test connection with timeout
        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeoutMs);

            var result = await adapter.TestConnectionAsync(decryptedFields, cts.Token);
            sw.Stop();

            return new CredentialTestResult
            {
                Success = result.IsSuccess,
                Message = result.IsSuccess
                    ? $"Connection successful. Products: {result.ProductCount ?? 0}"
                    : result.ErrorMessage ?? "Connection failed.",
                LatencyMs = (int)sw.ElapsedMilliseconds,
                Platform = platformCode
            };
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning(
                "Credential test timed out for Store {StoreId} (Platform: {Platform}) after {Ms}ms",
                request.StoreId, platformCode, sw.ElapsedMilliseconds);

            return new CredentialTestResult
            {
                Success = false,
                Message = $"Connection test timed out after {TimeoutMs}ms.",
                LatencyMs = (int)sw.ElapsedMilliseconds,
                Platform = platformCode
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Credential test failed for Store {StoreId} (Platform: {Platform})",
                request.StoreId, platformCode);

            return new CredentialTestResult
            {
                Success = false,
                Message = $"Connection test error: {ex.Message}",
                LatencyMs = (int)sw.ElapsedMilliseconds,
                Platform = platformCode
            };
        }
    }
}
