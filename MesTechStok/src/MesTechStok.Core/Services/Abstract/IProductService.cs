using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Services.Abstract;

/// <summary>
/// Ürün yönetimi için servis arayüzü
/// Barkodlu stok takip sisteminin ana ürün işlemlerini tanımlar
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Tüm aktif ürünleri getirir
    /// </summary>
    Task<IEnumerable<Product>> GetAllProductsAsync();

    /// <summary>
    /// ID'ye göre ürün getirir
    /// </summary>
    Task<Product?> GetProductByIdAsync(int id);

    /// <summary>
    /// Barkoda göre ürün arar - Barkod tarayıcı entegrasyonu için kritik
    /// </summary>
    Task<Product?> GetProductByBarcodeAsync(string barcode);

    /// <summary>
    /// SKU'ya göre ürün arar
    /// </summary>
    Task<Product?> GetProductBySkuAsync(string sku);

    /// <summary>
    /// Ürün adına göre arama yapar (fuzzy search)
    /// </summary>
    Task<IEnumerable<Product>> SearchProductsByNameAsync(string searchTerm);

    /// <summary>
    /// Kategoriye göre ürünleri getirir
    /// </summary>
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category);

    /// <summary>
    /// Stok seviyesi minimum seviyenin altında olan ürünleri getirir
    /// </summary>
    Task<IEnumerable<Product>> GetLowStockProductsAsync();

    /// <summary>
    /// Sayfalı ve filtreli ürün listeleme (büyük veri setleri için optimize edilmiştir)
    /// </summary>
    /// <param name="page">1 tabanlı sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına kayıt</param>
    /// <param name="searchTerm">Ad/Barcode/SKU araması (opsiyonel)</param>
    /// <param name="category">Kategori adı filtresi (opsiyonel)</param>
    /// <param name="sortBy">Sıralama alanı: Name|SalePrice|Stock|CreatedDate|Category (varsayılan: Name)</param>
    /// <param name="desc">Azalan sıralama (varsayılan: false)</param>
    /// <param name="inStock">Stokta olan/olmayan filtre (null=hepsi)</param>
    Task<PagedResult<Product>> GetProductsPagedAsync(int page, int pageSize, string? searchTerm = null, string? category = null, string? sortBy = "Name", bool desc = false, bool? inStock = null);

    /// <summary>
    /// Yeni ürün ekler
    /// </summary>
    Task<Product> CreateProductAsync(Product product);

    /// <summary>
    /// Ürün bilgilerini günceller
    /// </summary>
    Task<Product> UpdateProductAsync(Product product);

    /// <summary>
    /// Ürünü pasif yapar (fiziksel silme yapmaz)
    /// </summary>
    Task<bool> DeactivateProductAsync(int id);

    /// <summary>
    /// Ürünü aktif yapar
    /// </summary>
    Task<bool> ActivateProductAsync(int id);

    /// <summary>
    /// Ürün stok seviyesini günceller
    /// </summary>
    Task<bool> UpdateStockQuantityAsync(int productId, int newQuantity, string? notes = null);

    /// <summary>
    /// Ürün fiyatını günceller
    /// </summary>
    Task<bool> UpdateProductPriceAsync(int productId, decimal newPrice);

    /// <summary>
    /// Barkod benzersizliğini kontrol eder
    /// </summary>
    Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeProductId = null);

    /// <summary>
    /// SKU benzersizliğini kontrol eder
    /// </summary>
    Task<bool> IsSkuUniqueAsync(string sku, int? excludeProductId = null);

    /// <summary>
    /// Ürünün stok hareket geçmişini getirir
    /// </summary>
    Task<IEnumerable<StockMovement>> GetProductStockHistoryAsync(int productId);

    /// <summary>
    /// Toplu ürün güncelleme (Excel import vb. için)
    /// </summary>
    Task<bool> BulkUpdateProductsAsync(IEnumerable<Product> products);

    /// <summary>
    /// Toplam aktif ürün sayısını getirir
    /// Dashboard istatistikleri için gerekli
    /// </summary>
    Task<int> GetTotalCountAsync();
}
