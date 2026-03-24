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
}

public interface IOrderService
{
    Task<List<Order>> GetAllOrdersAsync();
    Task UpdateOrderAsync(Order order);
    Task CreateOrderAsync(Order order);
}

public interface IInventoryService
{
}
