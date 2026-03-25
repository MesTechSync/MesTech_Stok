using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product?> GetBySKUAsync(string sku);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<IReadOnlyList<Product>> GetLowStockAsync();
    Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId);
    Task<IReadOnlyList<Product>> SearchAsync(string searchTerm);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Guid id);
    Task<int> GetCountAsync();
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default);
}
