using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetBySKUAsync(string sku);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<IReadOnlyList<Product>> GetLowStockAsync();
    Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId);
    Task<IReadOnlyList<Product>> SearchAsync(string searchTerm);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
    Task<int> GetCountAsync();
}
