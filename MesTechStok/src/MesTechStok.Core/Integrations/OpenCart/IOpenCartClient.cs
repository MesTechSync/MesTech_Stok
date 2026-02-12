using MesTechStok.Core.Integrations.OpenCart.Dtos;
using OpenCartSyncResult = MesTechStok.Core.Integrations.OpenCart.Dtos.OpenCartSyncResult;

namespace MesTechStok.Core.Integrations.OpenCart;

/// <summary>
/// OpenCart API entegrasyonu için client arayüzü
/// Çift yönlü veri senkronizasyonu sağlar
/// </summary>
public interface IOpenCartClient
{
    /// <summary>
    /// API bağlantı durumu
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// OpenCart API bağlantısını test eder
    /// </summary>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// API bağlantısını başlatır
    /// </summary>
    Task<bool> ConnectAsync(string apiUrl, string apiKey);

    /// <summary>
    /// Bağlantıyı kapatır
    /// </summary>
    void Disconnect();

    #region Product Operations

    /// <summary>
    /// OpenCart'tan tüm ürünleri getirir
    /// </summary>
    Task<IEnumerable<OpenCartProduct>> GetAllProductsAsync();

    /// <summary>
    /// Belirli bir ürünü ID ile getirir
    /// </summary>
    Task<OpenCartProduct?> GetProductByIdAsync(int productId);

    /// <summary>
    /// Ürünü SKU ile arar
    /// </summary>
    Task<OpenCartProduct?> GetProductBySkuAsync(string sku);

    /// <summary>
    /// OpenCart'a yeni ürün ekler
    /// </summary>
    Task<int?> CreateProductAsync(OpenCartProduct product);

    /// <summary>
    /// OpenCart'ta ürün bilgilerini günceller
    /// </summary>
    Task<bool> UpdateProductAsync(int productId, OpenCartProduct product);

    /// <summary>
    /// OpenCart'ta ürün stok seviyesini günceller
    /// </summary>
    Task<bool> UpdateProductStockAsync(int productId, int quantity);

    /// <summary>
    /// OpenCart'ta ürün fiyatını günceller
    /// </summary>
    Task<bool> UpdateProductPriceAsync(int productId, decimal price);

    /// <summary>
    /// OpenCart'tan ürünü siler
    /// </summary>
    Task<bool> DeleteProductAsync(int productId);

    #endregion

    #region Order Operations

    /// <summary>
    /// OpenCart'tan tüm siparişleri getirir
    /// </summary>
    Task<IEnumerable<OpenCartOrder>> GetAllOrdersAsync(DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Belirli bir siparişi ID ile getirir
    /// </summary>
    Task<OpenCartOrder?> GetOrderByIdAsync(int orderId);

    /// <summary>
    /// Yeni siparişleri getirir (belirli tarihten sonra)
    /// </summary>
    Task<IEnumerable<OpenCartOrder>> GetNewOrdersAsync(DateTime fromDate);

    /// <summary>
    /// Sipariş durumunu günceller
    /// </summary>
    Task<bool> UpdateOrderStatusAsync(int orderId, string status);

    /// <summary>
    /// Sipariş durumuna göre siparişleri getirir
    /// </summary>
    Task<IEnumerable<OpenCartOrder>> GetOrdersByStatusAsync(string status);

    #endregion

    #region Category Operations

    /// <summary>
    /// OpenCart kategorilerini getirir
    /// </summary>
    Task<IEnumerable<OpenCartCategory>> GetCategoriesAsync();

    /// <summary>
    /// Yeni kategori oluşturur
    /// </summary>
    Task<int?> CreateCategoryAsync(OpenCartCategory category);

    #endregion

    #region Customer Operations

    /// <summary>
    /// Müşteri bilgilerini getirir
    /// </summary>
    Task<OpenCartCustomer?> GetCustomerByIdAsync(int customerId);

    /// <summary>
    /// E-mail ile müşteri arar
    /// </summary>
    Task<OpenCartCustomer?> GetCustomerByEmailAsync(string email);

    #endregion

    #region Sync Operations

    /// <summary>
    /// Son senkronizasyon tarihini getirir
    /// </summary>
    Task<DateTime?> GetLastSyncDateAsync(string syncType);

    /// <summary>
    /// Senkronizasyon tarihini günceller
    /// </summary>
    Task<bool> UpdateLastSyncDateAsync(string syncType, DateTime syncDate);

    /// <summary>
    /// Toplu ürün senkronizasyonu yapar
    /// </summary>
    Task<OpenCartSyncResult> BulkSyncProductsAsync(IEnumerable<OpenCartProduct> products);

    /// <summary>
    /// Toplu stok güncelleme yapar
    /// </summary>
    Task<OpenCartSyncResult> BulkUpdateStockAsync(IEnumerable<OpenCartStockUpdate> stockUpdates);

    #endregion

    #region Events

    /// <summary>
    /// API çağrısı başarılı olduğunda tetiklenir
    /// </summary>
    event EventHandler<ApiCallSuccessEventArgs>? ApiCallSuccess;

    /// <summary>
    /// API çağrısı başarısız olduğunda tetiklenir
    /// </summary>
    event EventHandler<ApiCallErrorEventArgs>? ApiCallError;

    /// <summary>
    /// Bağlantı durumu değiştiğinde tetiklenir
    /// </summary>
    event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

    #endregion
}

/// <summary>
/// OpenCart senkronizasyon sonucu
/// </summary>
// NOTE: Legacy duplicate class removed; alias defined at top.

/// <summary>
/// OpenCart stok güncelleme modeli
/// </summary>
public class OpenCartStockUpdate
{
    public int ProductId { get; set; }
    public string? Sku { get; set; }
    public int Quantity { get; set; }
    public bool InStock { get; set; }
}

/// <summary>
/// API çağrısı başarılı event args
/// </summary>
public class ApiCallSuccessEventArgs : EventArgs
{
    public string Method { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime CallTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// API çağrısı hata event args
/// </summary>
public class ApiCallErrorEventArgs : EventArgs
{
    public string Method { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public int? StatusCode { get; set; }
    public DateTime ErrorTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Bağlantı durumu değişiklik event args
/// </summary>
public class ConnectionStatusEventArgs : EventArgs
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public DateTime EventTime { get; set; } = DateTime.UtcNow;
}
