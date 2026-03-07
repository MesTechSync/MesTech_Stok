using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Product?> GetByIdAsync(int id)
        => await _context.Products.FindAsync(id).ConfigureAwait(false);

    public async Task<Product?> GetBySKUAsync(string sku)
        => await _context.Products.FirstOrDefaultAsync(p => p.SKU == sku).ConfigureAwait(false);

    public async Task<Product?> GetByBarcodeAsync(string barcode)
        => await _context.Products.FirstOrDefaultAsync(p => p.Barcode == barcode).ConfigureAwait(false);

    public async Task<IReadOnlyList<Product>> GetAllAsync()
        => await _context.Products.Where(p => p.IsActive).ToListAsync().ConfigureAwait(false);

    public async Task<IReadOnlyList<Product>> GetLowStockAsync()
        => await _context.Products.Where(p => p.IsActive && p.Stock <= p.MinimumStock).ToListAsync().ConfigureAwait(false);

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId)
        => await _context.Products.Where(p => p.CategoryId == categoryId && p.IsActive).ToListAsync().ConfigureAwait(false);

    public async Task<IReadOnlyList<Product>> SearchAsync(string searchTerm)
        => await _context.Products
            .Where(p => p.IsActive && (
                EF.Functions.ILike(p.Name, $"%{searchTerm}%") ||
                EF.Functions.ILike(p.SKU, $"%{searchTerm}%") ||
                (p.Barcode != null && EF.Functions.ILike(p.Barcode, $"%{searchTerm}%"))))
            .ToListAsync().ConfigureAwait(false);

    public async Task AddAsync(Product product)
        => await _context.Products.AddAsync(product).ConfigureAwait(false);

    public Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id).ConfigureAwait(false);
        if (product != null) _context.Products.Remove(product);
    }

    public async Task<int> GetCountAsync()
        => await _context.Products.CountAsync().ConfigureAwait(false);
}
