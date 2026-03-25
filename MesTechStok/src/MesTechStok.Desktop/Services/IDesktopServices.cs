using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password);
    Task LogoutAsync();
    Task<AuthUser?> GetCurrentUserAsync();
}

public class AuthResult
{
    public bool IsSuccess { get; set; }
    public bool Success => IsSuccess;
    public string? ErrorMessage { get; set; }
    public string? Message { get => ErrorMessage; set => ErrorMessage = value; }
    public string? Token { get; set; }
    public AuthUser? User { get; set; }
}

public class AuthUser
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public interface IDatabaseService
{
    Task<bool> IsDatabaseConnectedAsync();
    Task<DatabaseInfo> GetDatabaseInfoAsync();
    Task InitializeDatabaseAsync();
}

public interface IRealProductService
{
    Task<List<ProductItem>> GetAllProductsAsync();
    Task<List<ProductItem>> GetLowStockProductsAsync();
}

public interface IProductDataService
{
    Task<PagedResult<ProductItem>> GetProductsPagedAsync(
        int page, int pageSize, string? searchTerm = null,
        string? categoryFilter = null, ProductSortOrder sortOrder = ProductSortOrder.Name);
    Task<ProductItem?> GetProductByIdAsync(Guid id);
    Task<ProductItem?> GetProductByBarcodeAsync(string barcode);
    Task<ProductStatistics> GetStatisticsAsync();
    Task<bool> UpdateFinanceAsync(Guid id, decimal? salePrice = null, decimal? purchasePrice = null, decimal? discountRate = null);
    Task<bool> UpdateStockAsync(Guid id, int newStock);
    Task<bool> UpdateProductAsync(ProductItem product);
    Task<bool> AddProductAsync(ProductItem product);
    Task<bool> DeleteProductAsync(Guid id);
    Task<List<string>> GetCategoriesAsync();
}

public class ProductStatistics
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int LowStockCount => LowStockProducts;
    public int OutOfStockProducts { get; set; }
    public int CriticalStockCount { get; set; }
    public decimal TotalStockValue { get; set; }
    public decimal TotalValue => TotalStockValue;
    public decimal AveragePrice { get; set; }
}

public interface IInventoryDataService
{
    Task<PagedResult<InventoryItem>> GetInventoryPagedAsync(
        int page, int pageSize, string? searchTerm = null,
        StockStatusFilter statusFilter = StockStatusFilter.All,
        InventorySortOrder sortOrder = InventorySortOrder.ProductName);
    Task<InventoryStatistics> GetInventoryStatisticsAsync();
    Task<List<StockMovement>> GetRecentMovementsAsync(int count);
    Task<InventoryItem?> GetInventoryByBarcodeAsync(string barcode);
}

public interface ISystemResourceService
{
    SystemPerformance SystemPerformance { get; }
    event EventHandler<SystemPerformance>? PerformanceUpdated;
    Task<bool> IsSystemHealthyAsync();
    void Start();
    void Stop();
}

#pragma warning disable CS0618
public interface ITelemetryQueryService
{
    Task<IReadOnlyList<MesTechStok.Core.Data.Models.ApiCallLog>> GetRecentAsync(
        int take = 200, string? endpointContains = null,
        bool? success = null, string? category = null);
    Task<IReadOnlyList<MesTechStok.Core.Data.Models.CircuitStateLog>> GetCircuitStateHistoryAsync(int take = 100);
}
#pragma warning restore CS0618

public interface IApplicationMonitoringService
{
    Task StartAsync();
    Task StopAsync();
    void StartMonitoring();
    void RecordApplicationReady();
}

public interface IClickTrackingService
{
    void TrackClick(string elementName, string? category = null);
    void StartTracking();
}

// IAIConfigurationService is in MesTechStok.Core.Services namespace

public interface IOfflineQueueService
{
    Task EnqueueAsync(string operation, object data);
    Task ProcessQueueAsync();
}

public interface IOpenCartQueueWorker
{
    Task StartAsync();
    Task StopAsync();
    void Start();
    void Stop();
}

public interface IOpenCartInitializer
{
    Task InitializeAsync();
    void Initialize();
}

public interface IOpenCartHealthService
{
    Task<bool> IsHealthyAsync();
    int ConsecutiveFailures { get; }
}

public interface ITelemetryRetentionService
{
    Task CleanupOldRecordsAsync(int retentionDays = 30);
    Task ExecuteCleanupAsync();
}
