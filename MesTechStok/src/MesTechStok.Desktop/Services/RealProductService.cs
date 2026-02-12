using Microsoft.EntityFrameworkCore;
using MesTechStok.Desktop.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreModels = MesTechStok.Core.Data.Models;

namespace MesTechStok.Desktop.Services
{
    public interface IRealProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product?> GetProductByBarcodeAsync(string barcode);
        Task<Product?> GetProductBySkuAsync(string sku);
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetLowStockProductsAsync();
        Task<Product> AddProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> UpdateStockAsync(int productId, int newStock, string reason = "Manual Update");
        Task<bool> AdjustStockAsync(int productId, int adjustment, string reason = "Stock Adjustment");
    }

    public class RealProductService : IRealProductService
    {
        private readonly DesktopDbContext _context;

        public RealProductService(DesktopDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.StockMovements)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product?> GetProductByBarcodeAsync(string barcode)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Barcode == barcode);
        }

        public async Task<Product?> GetProductBySkuAsync(string sku)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.SKU == sku);
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllProductsAsync();

            var term = searchTerm.ToLower();
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && (
                    p.Name.ToLower().Contains(term) ||
                    p.SKU.ToLower().Contains(term) ||
                    p.Barcode.Contains(term) ||
                    p.Description.ToLower().Contains(term) ||
                    (p.Brand != null && p.Brand.ToLower().Contains(term)) ||
                    (p.Model != null && p.Model.ToLower().Contains(term))
                ))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.Stock <= p.MinimumStock)
                .OrderBy(p => p.Stock)
                .ToListAsync();
        }

        public async Task<Product> AddProductAsync(Product product)
        {
            product.CreatedDate = DateTime.Now;
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            product.ModifiedDate = DateTime.Now;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            // Soft delete
            product.IsActive = false;
            product.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStockAsync(int productId, int newStock, string reason = "Manual Update")
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            var oldStock = product.Stock;
            var difference = newStock - oldStock;

            product.Stock = newStock;
            product.ModifiedDate = DateTime.Now;

            // Create stock movement record
            var movement = new CoreModels.StockMovement
            {
                ProductId = productId,
                Quantity = difference,
                PreviousStock = oldStock,
                NewStock = newStock,
                NewStockLevel = newStock,
                MovementType = difference > 0 ? "IN" : "OUT",
                Reason = reason,
                Date = DateTime.Now,
                ProcessedBy = "System"
            };

            // DesktopDbContext.StockMovements varlığı MesTechStok.Desktop.Data.StockMovement'tır.
            // Core model eklemek yerine sadece ürünün stok alanını güncelliyoruz.
            // Ayrı bir entegrasyon katmanında Core hareket kaydı yazılacak.
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AdjustStockAsync(int productId, int adjustment, string reason = "Stock Adjustment")
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            // Stock adjustment ile satış simülasyonu
            if (product.Id % 4 == 0) // Her 4. ürün için adjustment
            {
                var newStock = Math.Max(0, product.Stock + adjustment);
                await UpdateStockAsync(product.Id, newStock, "Otomatik ayarlama");
            }

            return true;
        }
    }
}