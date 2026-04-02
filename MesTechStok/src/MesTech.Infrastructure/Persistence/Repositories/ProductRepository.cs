using MesTech.Domain.Common;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    // DEV6-TUR11: FindAsync bypasses global query filter — use FirstOrDefaultAsync instead
    // G349: Include children so FK dependency checks in DeleteProductHandler work
    public async Task<Product?> GetByIdAsync(Guid id)
        => await _context.Products
            .Include(p => p.OrderItems)
            .Include(p => p.PlatformMappings)
            .FirstOrDefaultAsync(p => p.Id == id).ConfigureAwait(false);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        => await _context.Products.Where(p => ids.Contains(p.Id)).ToListAsync(ct).ConfigureAwait(false);

    public async Task<Product?> GetBySKUAsync(string sku)
        => await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.SKU == sku).ConfigureAwait(false);

    public async Task<IReadOnlyList<Product>> GetBySKUsAsync(IEnumerable<string> skus, CancellationToken ct = default)
        => await _context.Products.Where(p => skus.Contains(p.SKU)).ToListAsync(ct).ConfigureAwait(false);

    public async Task<Product?> GetByBarcodeAsync(string barcode)
        => await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Barcode == barcode).ConfigureAwait(false);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
        => await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).Take(10000).AsNoTracking().ToListAsync(ct).ConfigureAwait(false); // G485: pagination guard

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken ct = default)
        => await _context.Products.Where(p => p.IsActive && p.Stock <= p.MinimumStock).Take(5000).AsNoTracking().ToListAsync(ct).ConfigureAwait(false); // G485: pagination guard

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId)
        => await _context.Products.Where(p => p.CategoryId == categoryId && p.IsActive).Take(5000).AsNoTracking().ToListAsync().ConfigureAwait(false); // G485: pagination guard

    public async Task<IReadOnlyList<Product>> SearchAsync(string searchTerm)
        => await _context.Products
            .Where(p => p.IsActive && (
                EF.Functions.ILike(p.Name, $"%{searchTerm}%") ||
                EF.Functions.ILike(p.SKU, $"%{searchTerm}%") ||
                (p.Barcode != null && EF.Functions.ILike(p.Barcode, $"%{searchTerm}%"))))
            .Take(1000) // G485: pagination guard — search results
            .AsNoTracking().ToListAsync().ConfigureAwait(false);

    public async Task AddAsync(Product product)
        => await _context.Products.AddAsync(product).ConfigureAwait(false);

    public Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id).ConfigureAwait(false);
        if (product != null) _context.Products.Remove(product);
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
        => await _context.Products.CountAsync(ct).ConfigureAwait(false);

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Products.CountAsync(p => p.TenantId == tenantId, ct).ConfigureAwait(false);

    // ── Dalga 4: Batch + Pagination ──

    public async Task<PagedResult<Product>> GetPagedAsync(int page = 1, int pageSize = 50, bool activeOnly = true)
    {
        var query = activeOnly ? _context.Products.Where(p => p.IsActive) : _context.Products.AsQueryable();
        var totalCount = await query.CountAsync().ConfigureAwait(false);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking().ToListAsync()
            .ConfigureAwait(false);
        return PagedResult<Product>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<Product>> GetBySKUsAsync(IEnumerable<string> skus)
    {
        var skuList = skus.ToList();
        return await _context.Products
            .Where(p => skuList.Contains(p.SKU))
            .AsNoTracking().ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task AddRangeAsync(IEnumerable<Product> products)
        => await _context.Products.AddRangeAsync(products).ConfigureAwait(false);

    public Task UpdateRangeAsync(IEnumerable<Product> products)
    {
        _context.Products.UpdateRange(products);
        return Task.CompletedTask;
    }

    public async Task BatchUpdateStockAsync(
        IReadOnlyList<(Guid ProductId, int NewStock)> updates,
        CancellationToken ct = default)
    {
        const int batchSize = 100;
        foreach (var batch in updates.Chunk(batchSize))
        {
            var ids = batch.Select(b => b.ProductId).ToList();
            var products = await _context.Products
                .Where(p => ids.Contains(p.Id))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var (productId, newStock) in batch)
            {
                var product = products.FirstOrDefault(p => p.Id == productId);
                if (product != null)
                {
                    var delta = newStock - product.Stock;
                    if (delta != 0)
                        product.AdjustStock(delta, Domain.Enums.StockMovementType.PlatformSync, "Batch sync");
                }
            }

            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task BatchUpdatePriceAsync(
        IReadOnlyList<(Guid ProductId, decimal NewPrice)> updates,
        CancellationToken ct = default)
    {
        const int batchSize = 100;
        foreach (var batch in updates.Chunk(batchSize))
        {
            var ids = batch.Select(b => b.ProductId).ToList();
            var products = await _context.Products
                .Where(p => ids.Contains(p.Id))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var (productId, newPrice) in batch)
            {
                var product = products.FirstOrDefault(p => p.Id == productId);
                product?.UpdatePrice(newPrice);
            }

            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task AddPlatformMappingAsync(ProductPlatformMapping mapping, CancellationToken ct = default)
    {
        await _context.ProductPlatformMappings.AddAsync(mapping, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Product>> GetByWarehouseAsync(Guid warehouseId, CancellationToken ct = default)
        => await _context.Products
            .Where(p => p.WarehouseId == warehouseId && p.IsActive)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
