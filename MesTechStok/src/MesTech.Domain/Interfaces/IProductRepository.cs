using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<Product?> GetBySKUAsync(string sku);
    Task<IReadOnlyList<Product>> GetBySKUsAsync(IEnumerable<string> skus, CancellationToken ct = default);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId);
    Task<IReadOnlyList<Product>> SearchAsync(string searchTerm);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Guid id);
    Task<int> GetCountAsync(CancellationToken ct = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddPlatformMappingAsync(ProductPlatformMapping mapping, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByWarehouseAsync(Guid warehouseId, CancellationToken ct = default);
}
