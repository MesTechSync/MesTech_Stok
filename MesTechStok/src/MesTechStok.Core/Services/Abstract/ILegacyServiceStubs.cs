using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Services.Abstract;

/// <summary>
/// Legacy service stubs — build fix only.
/// These interfaces were removed during AppDbContext elimination (DEV 1).
/// OpenCartSyncService still references them; will be fully migrated to CQRS.
/// </summary>
public interface IProductService
{
    Task<List<Product>> GetAllProductsAsync();
    Task UpdateProductAsync(Product product);
    Task CreateProductAsync(Product product);
    Task<List<Product>> GetLowStockProductsAsync();
    Task<Product?> GetProductByBarcodeAsync(string barcode);
    Task<int> GetTotalCountAsync();
}

public interface IOrderService
{
    Task<List<Order>> GetAllOrdersAsync();
    Task UpdateOrderAsync(Order order);
    Task CreateOrderAsync(Order order);
    Task UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus);
    Task<int> GetTotalCountAsync();
}

public interface IInventoryService
{
    Task AddStockWithLotAsync(Guid productId, int quantity, decimal? unitCost = null, string? lotNumber = null, DateTime? expiryDate = null, string? notes = null, string? ProcessedBy = null);
    Task<bool> RemoveStockAsync(Guid productId, int quantity, string? ProcessedBy = null);
    Task<bool> RemoveStockFefoAsync(Guid productId, int quantity, string? ProcessedBy = null);
    Task<List<object>> GetStockMovementsAsync(Guid productId, int? count = null);
    Task<List<StockMovement>> GetStockMovementsAsync(DateTime from, DateTime to);
}

public interface ICustomerService
{
    Task<List<Customer>> GetAllCustomersAsync();
    Task UpdateCustomerAsync(Customer customer);
    Task CreateCustomerAsync(Customer customer);
    Task<Customer?> GetCustomerByIdAsync(Guid id);
    Task<(List<Customer> Items, int TotalCount)> GetCustomersPagedAsync(int page, int pageSize, string? searchTerm = null);
    Task<CustomerStatistics> GetStatisticsAsync();
}

public interface ILoggingService
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? ex = null);
}

// IAIConfigurationService moved to MesTechStok.Core.Services namespace (CoreServiceStubs.cs)

/// <summary>
/// Core-level IAuthService — referenced by MainWindow.xaml.cs for DI resolution.
/// Implementation delegates to Desktop.Services.AuthService.
/// </summary>
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
}

public class AuthUser
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public interface IStockService
{
    Task<int> GetLowStockCountAsync();
}

public interface ILocationService
{
    Task<WarehouseZone> CreateZoneAsync(WarehouseZone zone);
    Task<WarehouseRack> CreateRackAsync(WarehouseRack rack);
    Task<WarehouseShelf> CreateShelfAsync(WarehouseShelf shelf);
    Task<WarehouseBin> CreateBinAsync(WarehouseBin bin);
    Task<bool> UpdateZoneAsync(WarehouseZone zone);
    Task<bool> UpdateRackAsync(WarehouseRack rack);
    Task<bool> UpdateShelfAsync(WarehouseShelf shelf);
    Task<bool> UpdateBinAsync(WarehouseBin bin);
    Task<List<WarehouseZone>> GetWarehouseZonesAsync(Guid warehouseId);
    Task<List<WarehouseRack>> GetRacksByZoneAsync(Guid zoneId);
    Task<List<WarehouseRack>> GetRacksByZoneAsync(int zoneId);
    Task<List<WarehouseShelf>> GetShelvesByRackAsync(Guid rackId);
    Task<List<WarehouseShelf>> GetShelvesByRackAsync(int rackId);
    Task<List<WarehouseBin>> GetBinsByShelfAsync(Guid shelfId);
    Task<List<WarehouseBin>> GetBinsByShelfAsync(int shelfId);
}

public interface IQRCodeService
{
    Task<byte[]> GenerateLocationQRCodeAsync(string binCode);
}

public interface IWarehouseOptimizationService
{
    Task<List<LocationSuggestion>> GetOptimalLocationSuggestionsAsync(Guid productId, int maxSuggestions);
}

public class LocationSuggestion
{
    public string Location { get; set; } = string.Empty;
    public string BinCode { get; set; } = string.Empty;
    public double Score { get; set; }
    public double MatchScore => Score;
    public string Reason { get; set; } = string.Empty;
}

public interface IMobileWarehouseService
{
    Task<List<MobileDevice>> GetActiveDevicesAsync();
}

public class MobileDevice
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
}

public class CustomerStatistics
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int VipCustomers { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}
