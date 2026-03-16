using MesTech.Domain.Entities;

namespace MesTech.Application.Interfaces;

/// <summary>
/// SyncLog entity erisim arayuzu — platform saglik durumu sorgulamasi icin.
/// </summary>
public interface ISyncLogRepository
{
    /// <summary>
    /// Belirtilen tenant icin her platform'un en son senkronizasyon logunu getirir.
    /// </summary>
    Task<IReadOnlyList<SyncLog>> GetLatestByPlatformAsync(
        Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen tarih araliginda basarisiz logların platform bazinda sayisini getirir.
    /// </summary>
    Task<IReadOnlyList<SyncLog>> GetFailedSinceAsync(
        Guid tenantId, DateTime since, CancellationToken cancellationToken = default);
}
