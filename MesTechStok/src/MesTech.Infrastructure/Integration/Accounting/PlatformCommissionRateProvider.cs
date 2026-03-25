using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Accounting;

/// <summary>
/// Settlement verilerinden dinamik komisyon orani tureten provider.
/// Her platformun en son settlement batch'inden gercek oran hesaplar.
/// Settlement yoksa null doner — CommissionCalculationService fallback kullanir.
/// </summary>
public sealed class PlatformCommissionRateProvider : ICommissionRateProvider
{
    private readonly ISettlementBatchRepository _settlementRepo;
    private readonly ILogger<PlatformCommissionRateProvider> _logger;

    // Platform bazli cache — settlement data sik degismez
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

    public PlatformCommissionRateProvider(
        ISettlementBatchRepository settlementRepo,
        ILogger<PlatformCommissionRateProvider> logger)
    {
        _settlementRepo = settlementRepo ?? throw new ArgumentNullException(nameof(settlementRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<CommissionRateInfo?> GetRateAsync(
        string platform,
        string? categoryId,
        CancellationToken cancellationToken = default)
        => GetRateAsync(Guid.Empty, platform, categoryId, cancellationToken);

    public async Task<CommissionRateInfo?> GetRateAsync(
        Guid tenantId,
        string platform,
        string? categoryId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);

        try
        {
            // Tenant ID: settlement repo tenant-filtered query kullanir.
            var batches = await _settlementRepo
                .GetByPlatformAsync(tenantId, platform, cancellationToken)
                .ConfigureAwait(false);

            if (batches is null || batches.Count == 0)
            {
                _logger.LogDebug(
                    "Settlement data yok — platform: {Platform}, fallback kullanilacak",
                    platform);
                return null;
            }

            // En son batch'i al (tarih siralama: PeriodEnd desc)
            var latestBatch = batches
                .OrderByDescending(b => b.PeriodEnd)
                .First();

            if (latestBatch.TotalGross <= 0)
            {
                _logger.LogWarning(
                    "Settlement batch {BatchId} icin GrossAmount sifir veya negatif — platform: {Platform}",
                    latestBatch.Id, platform);
                return null;
            }

            // Oran = TotalCommission / TotalGross
            var rate = Math.Round(latestBatch.TotalCommission / latestBatch.TotalGross, 4);

            // Negatif veya absurt oran kontrolu
            if (rate < 0 || rate > 0.50m)
            {
                _logger.LogWarning(
                    "Hesaplanan oran mantik disi — platform: {Platform}, rate: {Rate}, batch: {BatchId}",
                    platform, rate, latestBatch.Id);
                return null;
            }

            _logger.LogInformation(
                "Dinamik komisyon orani — platform: {Platform}, rate: {Rate:P2}, kaynak: settlement batch {BatchId} ({PeriodEnd:yyyy-MM-dd})",
                platform, rate, latestBatch.Id, latestBatch.PeriodEnd);

            return new CommissionRateInfo(
                Rate: rate,
                Type: CommissionType.Percentage,
                Source: $"{platform}Settlement",
                CachedUntil: DateTime.UtcNow.Add(CacheDuration));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Dinamik komisyon orani alinamadi — platform: {Platform}, fallback kullanilacak",
                platform);
            return null;
        }
    }
}
