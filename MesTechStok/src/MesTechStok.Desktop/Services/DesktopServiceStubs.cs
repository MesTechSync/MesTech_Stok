using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MesTechStok.Desktop.Models;
using MesTechStok.Core.Services.Abstract;

namespace MesTechStok.Desktop.Services;

#pragma warning disable CS0618 // Legacy service stubs — build fix only, will migrate to CQRS

public class TelemetryQueryService : ITelemetryQueryService
{
    public Task<IReadOnlyList<MesTechStok.Core.Data.Models.ApiCallLog>> GetRecentAsync(
        int take = 200, string? endpointContains = null,
        bool? success = null, string? category = null)
    {
        return Task.FromResult<IReadOnlyList<MesTechStok.Core.Data.Models.ApiCallLog>>(Array.Empty<MesTechStok.Core.Data.Models.ApiCallLog>());
    }

    public Task<IReadOnlyList<MesTechStok.Core.Data.Models.CircuitStateLog>> GetCircuitStateHistoryAsync(int take = 100)
    {
        return Task.FromResult<IReadOnlyList<MesTechStok.Core.Data.Models.CircuitStateLog>>(Array.Empty<MesTechStok.Core.Data.Models.CircuitStateLog>());
    }
}

public class LogAnalysisService
{
    private readonly ILogger<LogAnalysisService> _logger;

    public LogAnalysisService(ILogger<LogAnalysisService> logger)
    {
        _logger = logger;
    }

    public Task<string> AnalyzeAsync(string command)
    {
        _logger.LogInformation("Log analysis requested: {Command}", command);
        return Task.FromResult("Log analizi henüz yapılandırılmadı.");
    }

    public Task<LogAnalysisResult> AnalyzeEncodingIssuesAsync()
    {
        return Task.FromResult(new LogAnalysisResult());
    }

    public Task<bool> FixEncodingIssuesAsync()
    {
        return Task.FromResult(true);
    }
}

public class SqlBackedReportsService
{
    private readonly ILogger<SqlBackedReportsService> _logger;

    public SqlBackedReportsService(ILogger<SqlBackedReportsService> logger)
    {
        _logger = logger;
    }

    public Task<DashboardSummary> GetDashboardSummariesAsync()
    {
        return Task.FromResult(new DashboardSummary());
    }

    public Task<List<DailyRevenueItem>> GetDailyRevenueAsync(int days = 15)
    {
        return Task.FromResult(new List<DailyRevenueItem>());
    }
}

public class SystemResourceService : ISystemResourceService, IDisposable
{
    private readonly ILogger<SystemResourceService> _logger;
    private SystemPerformance _performance = new();

    public SystemResourceService(ILogger<SystemResourceService> logger)
    {
        _logger = logger;
    }

    public SystemPerformance SystemPerformance => _performance;
    public event EventHandler<SystemPerformance>? PerformanceUpdated;

    public Task<bool> IsSystemHealthyAsync() => Task.FromResult(true);
    public void Start() => _logger.LogInformation("System resource monitoring started");
    public void Stop() => _logger.LogInformation("System resource monitoring stopped");
    public Task ApplyThrottlingForNonMesTechAsync(double? threshold = null) => Task.CompletedTask;
    public void Dispose() { GC.SuppressFinalize(this); }
}

public class AuthService : IAuthService, MesTechStok.Core.Services.Abstract.IAuthService
{
    public Task<AuthResult> LoginAsync(string username, string password)
    {
        return Task.FromResult(new AuthResult { IsSuccess = false, ErrorMessage = "Auth service not configured" });
    }

    public Task LogoutAsync() => Task.CompletedTask;
    public Task<AuthUser?> GetCurrentUserAsync() => Task.FromResult<AuthUser?>(null);

    // Explicit implementation for Core.IAuthService
    Task<MesTechStok.Core.Services.Abstract.AuthResult> MesTechStok.Core.Services.Abstract.IAuthService.LoginAsync(string username, string password)
    {
        return Task.FromResult(new MesTechStok.Core.Services.Abstract.AuthResult { IsSuccess = false, ErrorMessage = "Auth service not configured" });
    }

    Task<MesTechStok.Core.Services.Abstract.AuthUser?> MesTechStok.Core.Services.Abstract.IAuthService.GetCurrentUserAsync()
    {
        return Task.FromResult<MesTechStok.Core.Services.Abstract.AuthUser?>(null);
    }
}

public class DatabaseService : IDatabaseService
{
    public Task<bool> IsDatabaseConnectedAsync() => Task.FromResult(false);
    public Task<DatabaseInfo> GetDatabaseInfoAsync() => Task.FromResult(new DatabaseInfo());
    public Task InitializeDatabaseAsync() => Task.CompletedTask;
}

public class SqlBackedProductService : IProductDataService
{
    public Task<PagedResult<ProductItem>> GetProductsPagedAsync(int page, int pageSize, string? searchTerm = null, string? categoryFilter = null, ProductSortOrder sortOrder = ProductSortOrder.Name)
        => Task.FromResult(new PagedResult<ProductItem>());
    public Task<ProductItem?> GetProductByIdAsync(Guid id) => Task.FromResult<ProductItem?>(null);
    public Task<ProductItem?> GetProductByBarcodeAsync(string barcode) => Task.FromResult<ProductItem?>(null);
    public Task<ProductStatistics> GetStatisticsAsync() => Task.FromResult(new ProductStatistics());
    public Task<bool> UpdateFinanceAsync(Guid id, decimal? salePrice = null, decimal? purchasePrice = null, decimal? discountRate = null) => Task.FromResult(false);
    public Task<bool> UpdateStockAsync(Guid id, int newStock) => Task.FromResult(false);
    public Task<bool> UpdateProductAsync(ProductItem product) => Task.FromResult(false);
    public Task<bool> AddProductAsync(ProductItem product) => Task.FromResult(false);
    public Task<bool> DeleteProductAsync(Guid id) => Task.FromResult(false);
    public Task<List<string>> GetCategoriesAsync() => Task.FromResult(new List<string>());
}

public class SqlBackedInventoryService : IInventoryDataService
{
    public Task<PagedResult<InventoryItem>> GetInventoryPagedAsync(int page, int pageSize, string? searchTerm = null, StockStatusFilter statusFilter = StockStatusFilter.All, InventorySortOrder sortOrder = InventorySortOrder.ProductName)
        => Task.FromResult(new PagedResult<InventoryItem>());
    public Task<InventoryStatistics> GetInventoryStatisticsAsync() => Task.FromResult(new InventoryStatistics());
    public Task<List<StockMovement>> GetRecentMovementsAsync(int count) => Task.FromResult(new List<StockMovement>());
    public Task<InventoryItem?> GetInventoryByBarcodeAsync(string barcode) => Task.FromResult<InventoryItem?>(null);
}

public class ImageStorageService
{
    public Task<string?> SaveImageAsync(string sourcePath, Guid productId) => Task.FromResult<string?>(null);
    public Task<ImageSaveResult> SaveAsync(Guid productId, string sourcePath, string? fileName = null)
        => Task.FromResult(new ImageSaveResult());
    public Task<bool> DeleteImageAsync(Guid productId) => Task.FromResult(true);
    public string? GetImagePath(Guid productId) => null;
    public string GetProductFolder(Guid productId)
    {
        var local = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        return System.IO.Path.Combine(local, "MesTechStok", "Images", "Products", productId.ToString());
    }
}

public class ImageSaveResult
{
    public string? Full1200 { get; set; }
    public string? Thumbnail { get; set; }
}

public class PdfReportService
{
    public Task<byte[]> GenerateReportAsync(string reportType, object data)
        => Task.FromResult(Array.Empty<byte>());
    public Task<byte[]> ExportLowStockReportAsync(string filePath = "")
        => Task.FromResult(Array.Empty<byte>());
    public Task<byte[]> ExportLowStockReportAsync(string filePath, string companyName, object data)
        => Task.FromResult(Array.Empty<byte>());
}

public class SimpleSecurityService
{
    public bool IsAuthenticated { get; set; }
    public string? CurrentUser { get; set; }
}

public class EnhancedProductService : IRealProductService, IProductDataService
{
    public Task<List<ProductItem>> GetAllProductsAsync() => Task.FromResult(new List<ProductItem>());
    public Task<List<ProductItem>> GetLowStockProductsAsync() => Task.FromResult(new List<ProductItem>());
    public Task<PagedResult<ProductItem>> GetProductsPagedAsync(int page, int pageSize, string? searchTerm = null, string? categoryFilter = null, ProductSortOrder sortOrder = ProductSortOrder.Name)
        => Task.FromResult(new PagedResult<ProductItem>());
    public Task<ProductItem?> GetProductByIdAsync(Guid id) => Task.FromResult<ProductItem?>(null);
    public Task<ProductItem?> GetProductByBarcodeAsync(string barcode) => Task.FromResult<ProductItem?>(null);
    public Task<ProductStatistics> GetStatisticsAsync() => Task.FromResult(new ProductStatistics());
    public Task<bool> UpdateFinanceAsync(Guid id, decimal? salePrice = null, decimal? purchasePrice = null, decimal? discountRate = null) => Task.FromResult(false);
    public Task<bool> UpdateStockAsync(Guid id, int newStock) => Task.FromResult(false);
    public Task<bool> UpdateProductAsync(ProductItem product) => Task.FromResult(false);
    public Task<bool> AddProductAsync(ProductItem product) => Task.FromResult(false);
    public Task<bool> DeleteProductAsync(Guid id) => Task.FromResult(false);
    public Task<List<string>> GetCategoriesAsync() => Task.FromResult(new List<string>());
}

public class TelemetryService : MesTechStok.Core.Interfaces.ITelemetryService
{
    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null) { }
    public void TrackMetric(string metricName, double value) { }
    public Task LogApiCallAsync(string endpoint, string method, bool success, int statusCode,
        int durationMs, string category, string correlationId) => Task.CompletedTask;
    public Task LogCircuitStateChangeAsync(string previousState, string newState, string reason,
        double failureRate, string correlationId) => Task.CompletedTask;
}

public class TelemetryRetentionService : ITelemetryRetentionService, Microsoft.Extensions.Hosting.IHostedService
{
    public Task CleanupOldRecordsAsync(int retentionDays = 30) => Task.CompletedTask;
    public Task ExecuteCleanupAsync() => CleanupOldRecordsAsync();
    public Task StartAsync(System.Threading.CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(System.Threading.CancellationToken cancellationToken) => Task.CompletedTask;
}

public class ApplicationMonitoringService : IApplicationMonitoringService
{
    public Task StartAsync() => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
    public void StartMonitoring() { }
    public void RecordApplicationReady() { }
}

public class ClickTrackingService : IClickTrackingService
{
    public void TrackClick(string elementName, string? category = null) { }
    public void StartTracking() { }
}

public class OfflineQueueService : IOfflineQueueService
{
    public Task EnqueueAsync(string operation, object data) => Task.CompletedTask;
    public Task ProcessQueueAsync() => Task.CompletedTask;
}

public class OpenCartQueueWorker : IOpenCartQueueWorker
{
    public Task StartAsync() => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
    public void Start() { }
    public void Stop() { }
}

public class OpenCartInitializer : IOpenCartInitializer
{
    public Task InitializeAsync() => Task.CompletedTask;
    public void Initialize() { }
}

public class OpenCartHealthService : IOpenCartHealthService
{
    public Task<bool> IsHealthyAsync() => Task.FromResult(true);
    public int ConsecutiveFailures => 0;
}

public class MockLocationService : ILocationService
{
    public Task<MesTechStok.Core.Data.Models.WarehouseZone> CreateZoneAsync(MesTechStok.Core.Data.Models.WarehouseZone zone) => Task.FromResult(zone);
    public Task<MesTechStok.Core.Data.Models.WarehouseRack> CreateRackAsync(MesTechStok.Core.Data.Models.WarehouseRack rack) => Task.FromResult(rack);
    public Task<MesTechStok.Core.Data.Models.WarehouseShelf> CreateShelfAsync(MesTechStok.Core.Data.Models.WarehouseShelf shelf) => Task.FromResult(shelf);
    public Task<MesTechStok.Core.Data.Models.WarehouseBin> CreateBinAsync(MesTechStok.Core.Data.Models.WarehouseBin bin) => Task.FromResult(bin);
    public Task<bool> UpdateZoneAsync(MesTechStok.Core.Data.Models.WarehouseZone zone) => Task.FromResult(true);
    public Task<bool> UpdateRackAsync(MesTechStok.Core.Data.Models.WarehouseRack rack) => Task.FromResult(true);
    public Task<bool> UpdateShelfAsync(MesTechStok.Core.Data.Models.WarehouseShelf shelf) => Task.FromResult(true);
    public Task<bool> UpdateBinAsync(MesTechStok.Core.Data.Models.WarehouseBin bin) => Task.FromResult(true);
    public Task<List<MesTechStok.Core.Data.Models.WarehouseZone>> GetWarehouseZonesAsync(Guid warehouseId) => Task.FromResult(new List<MesTechStok.Core.Data.Models.WarehouseZone>());
    public Task<List<MesTechStok.Core.Data.Models.WarehouseRack>> GetRacksByZoneAsync(Guid zoneId) => Task.FromResult(new List<MesTechStok.Core.Data.Models.WarehouseRack>());
    public Task<List<MesTechStok.Core.Data.Models.WarehouseRack>> GetRacksByZoneAsync(int zoneId) => Task.FromResult(new List<MesTechStok.Core.Data.Models.WarehouseRack>());
    public Task<List<MesTechStok.Core.Data.Models.WarehouseShelf>> GetShelvesByRackAsync(Guid rackId) => Task.FromResult(new List<MesTechStok.Core.Data.Models.WarehouseShelf>());
    public Task<List<MesTechStok.Core.Data.Models.WarehouseShelf>> GetShelvesByRackAsync(int rackId) => Task.FromResult(new List<MesTechStok.Core.Data.Models.WarehouseShelf>());
    public Task<List<MesTechStok.Core.Data.Models.WarehouseBin>> GetBinsByShelfAsync(Guid shelfId) => Task.FromResult(new List<MesTechStok.Core.Data.Models.WarehouseBin>());
    public Task<List<MesTechStok.Core.Data.Models.WarehouseBin>> GetBinsByShelfAsync(int shelfId) => Task.FromResult(new List<MesTechStok.Core.Data.Models.WarehouseBin>());
}

public class MockWarehouseOptimizationService : IWarehouseOptimizationService
{
    public Task<List<LocationSuggestion>> GetOptimalLocationSuggestionsAsync(Guid productId, int maxSuggestions)
        => Task.FromResult(new List<LocationSuggestion>());
}

public class MockMobileWarehouseService : IMobileWarehouseService
{
    public Task<List<MobileDevice>> GetActiveDevicesAsync() => Task.FromResult(new List<MobileDevice>());
}

public class DocumentStorageService
{
    public Task<string?> SaveDocumentAsync(string sourcePath, Guid entityId) => Task.FromResult<string?>(null);
    public Task<string?> SaveAsync(Guid entityId, string sourcePath) => Task.FromResult<string?>(null);
    public string GetCustomerFolder(Guid customerId)
    {
        var local = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        return System.IO.Path.Combine(local, "MesTechStok", "Documents", "Customers", customerId.ToString());
    }
}

public class ColorSwatchService
{
    public List<string> GetAvailableColors() => new() { "#000000", "#FFFFFF", "#FF0000", "#00FF00", "#0000FF" };
    public Task<string> GenerateAsync(string color, int size = 512, string? name = null)
    {
        var local = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        var path = System.IO.Path.Combine(local, "MesTechStok", "Swatches", $"{color.Replace("#", "")}.png");
        return Task.FromResult(path);
    }
}

public class NavigationTimingService
{
    public static NavigationTimingService Instance { get; } = new();
    public void StartNavigation(string viewName) { }
    public void EndNavigation(string viewName) { }
    public void StartTiming(string viewName) { }
    public void StopTiming(string viewName) { }
    public long GetLastNavigationMs() => 0;
    public void RecordNavigation(string viewName, long durationMs) { }
    public void Start(string viewName) { }
    public void Stop(string viewName) { }
    public string GetReport() => string.Empty;
}

public static class ProductUploadWindowManager
{
    public static void ShowUploadWindow() { }
    public static bool IsUploadWindowOpen { get; set; }
    public static bool TryOpen(params object?[] args) { ShowUploadWindow(); return true; }
    public static bool TryOpenWithBarcode(params object?[] args) { return true; }
}

public static class LogAnalyzer
{
    public static Task<string> AnalyzeAsync(string logContent) => Task.FromResult(string.Empty);
    public static LogErrorStats GetErrorStats(int hoursBack = 24) => new();
    public static List<LogErrorEntry> GetRecentCriticalErrors(int hoursBack = 24) => new();
}

public class LogErrorStats
{
    public int TotalCriticalErrors { get; set; }
    public int OfflineQueueErrors { get; set; }
    public int ImageStorageErrors { get; set; }
    public int EncodingErrors { get; set; }
    public int PathInjectionAttempts { get; set; }
    public bool IsHealthy { get; set; } = true;
}

public class LogErrorEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

#pragma warning restore CS0618
