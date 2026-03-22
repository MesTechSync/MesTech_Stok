namespace MesTech.Application.Interfaces;

/// <summary>
/// Distributed lock servisi — Redis veya benzeri mekanizma ile
/// birden fazla process/container arasında kaynak kilitleme.
/// Stok güncelleme, ödeme işleme gibi critical section'larda kullanılır.
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Belirtilen kaynak için distributed lock al.
    /// Lock alınamazsa null döner (timeout).
    /// </summary>
    /// <param name="resourceKey">Kilit anahtarı (örn: "stock:product:{productId}")</param>
    /// <param name="expiry">Kilidin otomatik süresi (default: 30sn)</param>
    /// <param name="waitTimeout">Kilit bekleme süresi (default: 10sn)</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>IAsyncDisposable lock handle, veya null (timeout)</returns>
    Task<IAsyncDisposable?> AcquireLockAsync(
        string resourceKey,
        TimeSpan expiry = default,
        TimeSpan waitTimeout = default,
        CancellationToken ct = default);
}
