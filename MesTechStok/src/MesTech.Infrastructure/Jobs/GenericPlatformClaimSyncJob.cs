using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Tüm platformlar için periyodik iade talebi çekme (claim pull) job'ı.
/// Platform kodu parametre olarak alınır — her platform ayrı Hangfire recurring job.
///
/// G498 FIX: Sadece TrendyolClaimSyncJob vardı, diğer platformların
/// iade talepleri webhook-only'ydi. Bu job periodic pull safety net sağlar.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class GenericPlatformClaimSyncJob
{
    private readonly IAdapterFactory _factory;
    private readonly ILogger<GenericPlatformClaimSyncJob> _logger;

    public GenericPlatformClaimSyncJob(
        IAdapterFactory factory,
        ILogger<GenericPlatformClaimSyncJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task ExecuteAsync(string platformCode, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[ClaimSync] {Platform} iade talebi sync başlıyor...", platformCode);

        var adapter = _factory.ResolveCapability<IClaimCapableAdapter>(platformCode);
        if (adapter is null)
        {
            _logger.LogWarning(
                "[ClaimSync] {Platform} IClaimCapableAdapter bulunamadı — atlaniyor", platformCode);
            return;
        }

        try
        {
            var since = DateTime.UtcNow.AddHours(-4);
            var claims = await adapter.PullClaimsAsync(since, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[ClaimSync] {Platform} TAMAMLANDI — {Count} iade talebi çekildi (son 4 saat)",
                platformCode, claims.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ClaimSync] {Platform} iade talebi sync BAŞARISIZ", platformCode);
            throw; // Hangfire retry
        }
    }
}
