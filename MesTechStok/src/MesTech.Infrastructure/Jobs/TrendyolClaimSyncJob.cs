using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 15 dakikada Trendyol iade bildirimlerini ceker ve DB'ye persist eder.
/// Akis: PullClaimsAsync → duplicate check → ExternalClaimDto→ReturnRequest mapping → AddAsync → SaveChanges.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class TrendyolClaimSyncJob : ISyncJob
{
    public string JobId => "trendyol-claim-sync";
    public string CronExpression => "*/15 * * * *"; // Her 15 dk

    private readonly IAdapterFactory _factory;
    private readonly IReturnRequestRepository _returnRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TrendyolClaimSyncJob> _logger;

    public TrendyolClaimSyncJob(
        IAdapterFactory factory,
        IReturnRequestRepository returnRepo,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ILogger<TrendyolClaimSyncJob> logger)
    {
        _factory = factory;
        _returnRepo = returnRepo;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Trendyol iade sync basliyor...", JobId);

        try
        {
            var adapter = _factory.ResolveCapability<IClaimCapableAdapter>("Trendyol");
            if (adapter == null)
            {
                _logger.LogWarning("[{JobId}] Trendyol IClaimCapableAdapter bulunamadi, atlaniyor", JobId);
                return;
            }

            var since = DateTime.UtcNow.AddMinutes(-20);
            var claims = await adapter.PullClaimsAsync(since, ct).ConfigureAwait(false);

            if (claims.Count == 0)
            {
                _logger.LogDebug("[{JobId}] Yeni iade talebi yok (son 20dk)", JobId);
                return;
            }

            var tenantId = _tenantProvider.GetCurrentTenantId();
            int created = 0, updated = 0, skipped = 0;

            // Mevcut iadeleri tek sorguda çek (N+1 query önleme)
            var existingReturns = await _returnRepo.GetByTenantAsync(tenantId, 5000, ct).ConfigureAwait(false);
            var existingMap = existingReturns
                .Where(r => r.PlatformReturnId is not null)
                .ToDictionary(r => r.PlatformReturnId!, r => r);

            foreach (var dto in claims)
            {
                if (existingMap.TryGetValue(dto.PlatformClaimId, out var existing))
                {
                    // Status/notes değişmişse güncelle
                    var newNotes = $"Trendyol claim sync — {dto.Status}";
                    if (existing.Notes != newNotes)
                    {
                        existing.Notes = newNotes;
                        await _returnRepo.UpdateAsync(existing, ct).ConfigureAwait(false);
                        updated++;
                    }
                    else
                    {
                        skipped++;
                    }
                    continue;
                }

                var returnReq = new ReturnRequest
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PlatformReturnId = dto.PlatformClaimId,
                    Platform = PlatformType.Trendyol,
                    CustomerName = dto.CustomerName ?? "Trendyol Customer",
                    ReasonDetail = dto.Reason,
                    RequestDate = dto.ClaimDate,
                    Notes = $"Trendyol claim sync — {dto.Status}"
                };

                await _returnRepo.AddAsync(returnReq, ct).ConfigureAwait(false);
                existingMap[dto.PlatformClaimId] = returnReq;
                created++;
            }

            if (created > 0 || updated > 0)
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] Trendyol iade sync tamamlandi: {Total} cekildi, {Created} yeni, {Updated} guncellendi, {Skipped} degismemis",
                JobId, claims.Count, created, updated, skipped);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[{JobId}] Trendyol iade sync HATA", JobId);
            throw;
        }
    }
}
