using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Integrations.OpenCart.Dtos;

namespace MesTechStok.Core.Integrations.OpenCart;

/// <summary>
/// OpenCart senkronizasyon servisi arayüzü
/// Envanter sistemi ile OpenCart arasındaki çift yönlü veri senkronizasyonunu yönetir
/// </summary>
public interface IOpenCartSyncService
{
    /// <summary>
    /// Senkronizasyon durumu
    /// </summary>
    bool IsSyncRunning { get; }

    /// <summary>
    /// Son senkronizasyon tarihi
    /// </summary>
    DateTime? LastSyncDate { get; }

    /// <summary>
    /// Ürünleri OpenCart'a senkronize eder
    /// </summary>
    Task<OpenCartSyncResult> SyncProductsToOpenCartAsync();

    /// <summary>
    /// OpenCart'tan ürünleri alır ve yerel sisteme senkronize eder
    /// </summary>
    Task<OpenCartSyncResult> SyncProductsFromOpenCartAsync();

    /// <summary>
    /// Stok seviyelerini OpenCart'a senkronize eder
    /// </summary>
    Task<OpenCartSyncResult> SyncStockLevelsAsync();

    /// <summary>
    /// OpenCart'tan siparişleri alır
    /// </summary>
    Task<OpenCartSyncResult> SyncOrdersFromOpenCartAsync();

    /// <summary>
    /// Sipariş durumlarını OpenCart'a gönderir
    /// </summary>
    Task<OpenCartSyncResult> SyncOrderStatusToOpenCartAsync();

    /// <summary>
    /// Tam senkronizasyon yapar (ürünler, stoklar, siparişler)
    /// </summary>
    Task<OpenCartSyncResult> FullSyncAsync();

    /// <summary>
    /// Otomatik senkronizasyonu başlatır
    /// </summary>
    Task<bool> StartAutoSyncAsync(TimeSpan interval);

    /// <summary>
    /// Otomatik senkronizasyonu durdurur
    /// </summary>
    Task<bool> StopAutoSyncAsync();

    /// <summary>
    /// Senkronizasyon başladığında tetiklenir
    /// </summary>
    event EventHandler<SyncStartedEventArgs>? SyncStarted;

    /// <summary>
    /// Senkronizasyon tamamlandığında tetiklenir
    /// </summary>
    event EventHandler<SyncCompletedEventArgs>? SyncCompleted;

    /// <summary>
    /// Senkronizasyon hatası oluştuğunda tetiklenir
    /// </summary>
    event EventHandler<SyncErrorEventArgs>? SyncError;
}

/// <summary>
/// Senkronizasyon başlama event args
/// </summary>
public class SyncStartedEventArgs : EventArgs
{
    public string SyncType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Senkronizasyon tamamlanma event args
/// </summary>
public class SyncCompletedEventArgs : EventArgs
{
    public string SyncType { get; set; } = string.Empty;
    public OpenCartSyncResult Result { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Senkronizasyon hata event args
/// </summary>
public class SyncErrorEventArgs : EventArgs
{
    public string SyncType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime ErrorTime { get; set; } = DateTime.UtcNow;
}
