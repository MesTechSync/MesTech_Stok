using MesTech.Domain.Common;
using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<Product?> GetBySKUAsync(string sku, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetBySKUsAsync(IEnumerable<string> skus, CancellationToken ct = default);
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> SearchAsync(string searchTerm, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddPlatformMappingAsync(ProductPlatformMapping mapping, CancellationToken ct = default);
    Task<PagedResult<Product>> GetPagedAsync(int page = 1, int pageSize = 50, bool activeOnly = true, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByWarehouseAsync(Guid warehouseId, CancellationToken ct = default);
}
