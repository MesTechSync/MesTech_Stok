using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Services.Abstract;

/// <summary>
/// Envanter yönetimi için servis arayüzü
/// Stok hareketleri, barkod tarama ve gerçek zamanlı güncellemeler
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Barkod tarayarak stok çıkışı (satış) yapar
    /// </summary>
    Task<StockMovement?> ProcessBarcodeSaleAsync(string barcode, int quantity, string? DocumentNumber = null, string? notes = null);

    /// <summary>
    /// Barkod tarayarak stok girişi yapar
    /// </summary>
    Task<StockMovement?> ProcessBarcodeReceiveAsync(string barcode, int quantity, string? DocumentNumber = null, string? notes = null);

    /// <summary>
    /// Manuel stok girişi yapar
    /// </summary>
    Task<StockMovement> AddStockAsync(int productId, int quantity, string? DocumentNumber = null, string? notes = null, string? ProcessedBy = null);

    /// <summary>
    /// Manuel stok girişi yapar ve gelen partinin birim maliyetini dikkate alarak ağırlıklı ortalama maliyeti günceller.
    /// </summary>
    /// <param name="productId">Ürün ID</param>
    /// <param name="quantity">Giriş miktarı</param>
    /// <param name="unitCost">Gelen partinin birim maliyeti</param>
    /// <param name="DocumentNumber">Belge numarası (opsiyonel)</param>
    /// <param name="notes">Not (opsiyonel)</param>
    /// <param name="ProcessedBy">İşlemi yapan (opsiyonel)</param>
    Task<StockMovement> AddStockAsync(int productId, int quantity, decimal unitCost, string? DocumentNumber = null, string? notes = null, string? ProcessedBy = null);

    /// <summary>
    /// Parti/Lot ile stok girişi (FEFO desteği için). Lot numarası ve opsiyonel SKT ile giriş.
    /// </summary>
    Task<StockMovement> AddStockWithLotAsync(int productId, int quantity, decimal unitCost, string lotNumber, DateTime? expiryDate = null, string? DocumentNumber = null, string? notes = null, string? ProcessedBy = null);

    /// <summary>
    /// FEFO ile stok çıkışı: en erken SKT'li açık lotlardan karşılar; eşitlikte FIFO.
    /// </summary>
    Task<StockMovement> RemoveStockFefoAsync(int productId, int quantity, string? DocumentNumber = null, string? notes = null, string? ProcessedBy = null);

    /// <summary>
    /// Manuel stok çıkışı yapar
    /// </summary>
    Task<StockMovement> RemoveStockAsync(int productId, int quantity, string? DocumentNumber = null, string? notes = null, string? ProcessedBy = null);

    /// <summary>
    /// Stok düzeltmesi yapar (fiziksel sayım sonrası)
    /// </summary>
    Task<StockMovement> AdjustStockAsync(int productId, int newQuantity, string? notes = null, string? ProcessedBy = null);

    /// <summary>
    /// Ürünün mevcut stok seviyesini getirir
    /// </summary>
    Task<int> GetCurrentStockAsync(int productId);

    /// <summary>
    /// Barkoda göre ürünün mevcut stok seviyesini getirir
    /// </summary>
    Task<int> GetCurrentStockByBarcodeAsync(string barcode);

    /// <summary>
    /// Belirli bir tarih aralığındaki stok hareketlerini getirir
    /// </summary>
    Task<IEnumerable<StockMovement>> GetStockMovementsAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Belirli bir ürünün stok hareket geçmişini getirir
    /// </summary>
    Task<IEnumerable<StockMovement>> GetProductStockMovementsAsync(int productId, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Stok seviyesi kritik olan ürünleri getirir
    /// </summary>
    Task<IEnumerable<Product>> GetCriticalStockProductsAsync();

    /// <summary>
    /// Stok değeri hesaplar (maliyet bazında)
    /// </summary>
    Task<decimal> CalculateInventoryValueAsync(bool useCostPrice = true);

    /// <summary>
    /// Belirli kategorideki ürünlerin stok değerini hesaplar
    /// </summary>
    Task<decimal> CalculateCategoryInventoryValueAsync(string category, bool useCostPrice = true);

    /// <summary>
    /// Stok hareket türüne göre hareketleri getirir
    /// </summary>
    Task<IEnumerable<StockMovement>> GetMovementsByTypeAsync(StockMovementType movementType, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Son N günde yapılan stok hareketlerini getirir
    /// </summary>
    Task<IEnumerable<StockMovement>> GetRecentMovementsAsync(int days = 7);

    /// <summary>
    /// Envanter raporu oluşturur
    /// </summary>
    Task<InventoryReport> GenerateInventoryReportAsync(DateTime? asOfDate = null);

    /// <summary>
    /// Stok hareketi iptal eder (eğer mümkünse)
    /// </summary>
    Task<bool> CancelStockMovementAsync(int movementId, string? reason = null);

    /// <summary>
    /// Toplu stok güncellemesi yapar
    /// </summary>
    Task<IEnumerable<StockMovement>> BulkStockUpdateAsync(IEnumerable<BulkStockUpdate> updates, string? ProcessedBy = null);
}

/// <summary>
/// Envanter raporu model sınıfı
/// </summary>
public class InventoryReport
{
    public DateTime ReportDate { get; set; }
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public decimal TotalCostValue { get; set; }
    public IEnumerable<ProductStockSummary> ProductSummaries { get; set; } = new List<ProductStockSummary>();
}

/// <summary>
/// Ürün stok özeti model sınıfı
/// </summary>
public class ProductStockSummary
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinStockLevel { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public bool IsLowStock => CurrentStock <= MinStockLevel;
    public bool IsOutOfStock => CurrentStock <= 0;
}

/// <summary>
/// Toplu stok güncelleme için model sınıfı
/// </summary>
public class BulkStockUpdate
{
    public int ProductId { get; set; }
    public string? Barcode { get; set; }
    public int NewQuantity { get; set; }
    public StockMovementType MovementType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Notes { get; set; }
}
