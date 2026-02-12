using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Services.Abstract
{
    /// <summary>
    /// ALPHA TEAM: Stock management service interface
    /// Stok seviyesi kontrolü, düşük stok takibi ve stok hareketleri
    /// </summary>
    public interface IStockService
    {
        /// <summary>
        /// Toplam ürün sayısını döndürür
        /// </summary>
        /// <returns>Toplam ürün sayısı</returns>
        Task<int> GetTotalCountAsync();

        /// <summary>
        /// Düşük stok seviyesindeki ürün sayısını döndürür
        /// </summary>
        /// <returns>Düşük stoklu ürün sayısı</returns>
        Task<int> GetLowStockCountAsync();

        /// <summary>
        /// Kritik stok seviyesindeki ürün sayısını döndürür
        /// </summary>
        /// <returns>Kritik stoklu ürün sayısı</returns>
        Task<int> GetCriticalStockCountAsync();

        /// <summary>
        /// Stokta bulunmayan ürün sayısını döndürür
        /// </summary>
        /// <returns>Stoksuz ürün sayısı</returns>
        Task<int> GetOutOfStockCountAsync();

        /// <summary>
        /// Ürünün güncel stok miktarını döndürür
        /// </summary>
        /// <param name="productId">Ürün ID</param>
        /// <returns>Stok miktarı</returns>
        Task<int> GetProductStockAsync(int productId);

        /// <summary>
        /// Ürünün stok durumunu günceller
        /// </summary>
        /// <param name="productId">Ürün ID</param>
        /// <param name="quantity">Yeni stok miktarı</param>
        /// <param name="reason">Stok güncelleme nedeni</param>
        /// <returns>İşlem başarılı mı</returns>
        Task<bool> UpdateProductStockAsync(int productId, int quantity, string reason = "Manuel güncelleme");

        /// <summary>
        /// Stok seviyesi ayarlarını kontrol eder
        /// </summary>
        /// <param name="productId">Ürün ID</param>
        /// <returns>Stok durumu (Normal, Düşük, Kritik, Tükendi)</returns>
        Task<StockStatus> CheckStockLevelAsync(int productId);

        /// <summary>
        /// Düşük stoklu ürünlerin listesini döndürür
        /// </summary>
        /// <param name="threshold">Düşük stok eşiği (varsayılan: 10)</param>
        /// <returns>Düşük stoklu ürünler</returns>
        Task<List<Product>> GetLowStockProductsAsync(int threshold = 10);

        /// <summary>
        /// Stok hareketlerini kaydeder
        /// </summary>
        /// <param name="productId">Ürün ID</param>
        /// <param name="changeAmount">Değişiklik miktarı (+ veya -)</param>
        /// <param name="movementType">Hareket tipi (Giriş, Çıkış, Düzeltme, vb.)</param>
        /// <param name="reason">Hareket nedeni</param>
        /// <returns>İşlem başarılı mı</returns>
        Task<bool> RecordStockMovementAsync(int productId, int changeAmount, StockMovementType movementType, string reason);

        /// <summary>
        /// Son stok hareketlerini getirir
        /// </summary>
        /// <param name="limit">Gösterilecek kayıt sayısı</param>
        /// <returns>Son stok hareketleri</returns>
        Task<List<StockMovement>> GetRecentStockMovementsAsync(int limit = 50);
    }

    /// <summary>
    /// Stok durumu enum'u
    /// </summary>
    public enum StockStatus
    {
        Normal = 1,
        Low = 2,
        Critical = 3,
        OutOfStock = 4
    }
}
