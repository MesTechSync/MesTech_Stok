using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Tüm platformlar için periyodik iade talebi çekme + DB persist.
/// Platform kodu parametre olarak alınır — her platform ayrı Hangfire recurring job.
/// Akis: PullClaimsAsync → duplicate check → ExternalClaimDto→ReturnRequest → AddAsync → SaveChanges.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class GenericPlatformClaimSyncJob
{
    private readonly IAdapterFactory _factory;
    private readonly IReturnRequestRepository _returnRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<GenericPlatformClaimSyncJob> _logger;

    public GenericPlatformClaimSyncJob(
        IAdapterFactory factory,
        IReturnRequestRepository returnRepo,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ILogger<GenericPlatformClaimSyncJob> logger)
    {
        _factory = factory;
        _returnRepo = returnRepo;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(string platformCode, CancellationToken ct = default)
    {
        _logger.LogInformation("[ClaimSync] {Platform} iade talebi sync başlıyor...", platformCode);

        var adapter = _factory.ResolveCapability<IClaimCapableAdapter>(platformCode);
        if (adapter is null)
        {
            _logger.LogWarning("[ClaimSync] {Platform} IClaimCapableAdapter bulunamadı — atlaniyor", platformCode);
            return;
        }

        try
        {
            var since = DateTime.UtcNow.AddHours(-4);
            var claims = await adapter.PullClaimsAsync(since, ct).ConfigureAwait(false);

            if (claims.Count == 0)
            {
                _logger.LogDebug("[ClaimSync] {Platform} yeni iade talebi yok (son 4 saat)", platformCode);
                return;
            }

            var tenantId = _tenantProvider.GetCurrentTenantId();
            var platformType = Enum.TryParse<PlatformType>(platformCode, true, out var pt) ? pt : PlatformType.Trendyol;
            int created = 0, skipped = 0;

            var existingReturns = await _returnRepo.GetByTenantAsync(tenantId, 5000, ct).ConfigureAwait(false);
            var existingIds = new HashSet<string>(existingReturns
                .Where(r => r.PlatformReturnId is not null)
                .Select(r => r.PlatformReturnId!));

            foreach (var dto in claims)
            {
                if (existingIds.Contains(dto.PlatformClaimId))
                {
                    skipped++;
                    continue;
                }

                var returnReq = new ReturnRequest
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PlatformReturnId = dto.PlatformClaimId,
                    Platform = platformType,
                    CustomerName = dto.CustomerName ?? $"{platformCode} Customer",
                    ReasonDetail = dto.Reason,
                    RequestDate = dto.ClaimDate,
                    Notes = $"{platformCode} claim sync — {dto.Status}"
                };

                await _returnRepo.AddAsync(returnReq, ct).ConfigureAwait(false);
                existingIds.Add(dto.PlatformClaimId);
                created++;
            }

            if (created > 0)
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[ClaimSync] {Platform} TAMAMLANDI — {Total} çekildi, {Created} oluşturuldu, {Skipped} mevcut",
                platformCode, claims.Count, created, skipped);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[ClaimSync] {Platform} iade talebi sync BAŞARISIZ", platformCode);
            throw;
        }
    }
}
