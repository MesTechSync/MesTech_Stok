using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MesTechStok.Core.Data.Models;
#pragma warning disable CS0618

namespace MesTechStok.Core.Services.Concrete
{
    /// <summary>
    /// Legacy concrete service stubs — build fix only.
    /// Will be migrated to CQRS handlers.
    /// </summary>
    public class ProductService : Abstract.IProductService
    {
        public Task<List<Product>> GetAllProductsAsync() => Task.FromResult(new List<Product>());
        public Task UpdateProductAsync(Product product) => Task.CompletedTask;
        public Task CreateProductAsync(Product product) => Task.CompletedTask;
        public Task<List<Product>> GetLowStockProductsAsync() => Task.FromResult(new List<Product>());
        public Task<Product?> GetProductByBarcodeAsync(string barcode) => Task.FromResult<Product?>(null);
        public Task<int> GetTotalCountAsync() => Task.FromResult(0);
    }

    public class OrderService : Abstract.IOrderService
    {
        public Task<List<Order>> GetAllOrdersAsync() => Task.FromResult(new List<Order>());
        public Task UpdateOrderAsync(Order order) => Task.CompletedTask;
        public Task CreateOrderAsync(Order order) => Task.CompletedTask;
        public Task UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus) => Task.CompletedTask;
        public Task<int> GetTotalCountAsync() => Task.FromResult(0);
    }

    public class InventoryService : Abstract.IInventoryService
    {
        public Task AddStockWithLotAsync(Guid productId, int quantity, decimal? unitCost = null, string? lotNumber = null, DateTime? expiryDate = null, string? notes = null, string? ProcessedBy = null) => Task.CompletedTask;
        public Task<bool> RemoveStockAsync(Guid productId, int quantity, string? ProcessedBy = null) => Task.FromResult(true);
        public Task<bool> RemoveStockFefoAsync(Guid productId, int quantity, string? ProcessedBy = null) => Task.FromResult(true);
        public Task<List<object>> GetStockMovementsAsync(Guid productId, int? count = null) => Task.FromResult(new List<object>());
        public Task<List<MesTechStok.Core.Data.Models.StockMovement>> GetStockMovementsAsync(DateTime from, DateTime to) => Task.FromResult(new List<MesTechStok.Core.Data.Models.StockMovement>());
    }

    public class StockService : Abstract.IStockService
    {
        public Task<int> GetLowStockCountAsync() => Task.FromResult(0);
    }

    public class QRCodeService : Abstract.IQRCodeService
    {
        public Task<byte[]> GenerateLocationQRCodeAsync(string binCode) => Task.FromResult(Array.Empty<byte>());
    }

    public class LoggingService : Abstract.ILoggingService
    {
        public void LogInfo(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message, Exception? ex = null) { }
    }

    public class CustomerService : Abstract.ICustomerService
    {
        public Task<List<Customer>> GetAllCustomersAsync() => Task.FromResult(new List<Customer>());
        public Task UpdateCustomerAsync(Customer customer) => Task.CompletedTask;
        public Task CreateCustomerAsync(Customer customer) => Task.CompletedTask;
        public Task<Customer?> GetCustomerByIdAsync(Guid id) => Task.FromResult<Customer?>(null);
        public Task<(List<Customer> Items, int TotalCount)> GetCustomersPagedAsync(int page, int pageSize, string? searchTerm = null)
            => Task.FromResult((new List<Customer>(), 0));
        public Task<Abstract.CustomerStatistics> GetStatisticsAsync() => Task.FromResult(new Abstract.CustomerStatistics());
    }
}

namespace MesTechStok.Core.Services
{
    public interface IAIConfigurationService
    {
        bool IsConfigured { get; }
        string? ApiKey { get; set; }
        Task<bool> TestConnectionAsync();
        Task<List<AIConfiguration>> GetAllConfigurationsAsync();
        Task<AIConfiguration?> GetConfigurationAsync(int id);
        Task SaveConfigurationAsync(AIConfiguration config);
        Task<bool> TestConnectionAsync(int configId);
    }

    /// <summary>
    /// Re-export in MesTechStok.Core.Services namespace for DI registration compatibility.
    /// </summary>
    public class AIConfigurationService : IAIConfigurationService
    {
        public bool IsConfigured { get; set; }
        public string? ApiKey { get; set; }
        public Task<bool> TestConnectionAsync() => Task.FromResult(false);
        public Task<List<AIConfiguration>> GetAllConfigurationsAsync() => Task.FromResult(new List<AIConfiguration>());
        public Task<AIConfiguration?> GetConfigurationAsync(int id) => Task.FromResult<AIConfiguration?>(null);
        public Task SaveConfigurationAsync(AIConfiguration config) => Task.CompletedTask;
        public Task<bool> TestConnectionAsync(int configId) => Task.FromResult(false);
    }
}

namespace MesTechStok.Core.Services.Weather
{
    public class WeatherApiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.openweathermap.org/data/2.5";
        public string DefaultCity { get; set; } = "Istanbul";
    }

    public interface IWeatherService
    {
        Task<WeatherInfo> GetCurrentWeatherAsync(string city = "Istanbul");
    }

    public class WeatherService : IWeatherService
    {
        public Task<WeatherInfo> GetCurrentWeatherAsync(string city = "Istanbul")
            => Task.FromResult(new WeatherInfo());
    }

    public class OpenWeatherMapService : IWeatherService
    {
        public OpenWeatherMapService(System.Net.Http.HttpClient httpClient) { }
        public Task<WeatherInfo> GetCurrentWeatherAsync(string city = "Istanbul")
            => Task.FromResult(new WeatherInfo());
    }

    public class WeatherInfo
    {
        public string City { get; set; } = "Istanbul";
        public double Temperature { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}

namespace MesTechStok.Core.Services.Barcode
{
    public class BarcodeValidationSettings
    {
        public string ApiUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    public interface IBarcodeValidationService
    {
        Task<bool> ValidateAsync(string barcode);
    }

    public class GS1BarcodeValidationService : IBarcodeValidationService
    {
        public GS1BarcodeValidationService(System.Net.Http.HttpClient httpClient) { }
        public Task<bool> ValidateAsync(string barcode) => Task.FromResult(true);
    }

    public class BarcodeGeneratorService
    {
        public byte[] Generate(string content, int width = 200, int height = 100) => Array.Empty<byte>();
    }
}

#pragma warning restore CS0618
