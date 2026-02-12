using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MesTechStok.Core.Services.Abstract;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Data;

namespace MesTechStok.Core.Services.Concrete
{
    /// <summary>
    /// ALPHA TEAM: Stock management service implementation
    /// Stok yönetimi, seviye kontrolü ve hareket takibi
    /// </summary>
    public class StockService : IStockService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StockService> _logger;

        public StockService(AppDbContext context, ILogger<StockService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Toplam ürün sayısını döndürür
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            try
            {
                return await _context.Products.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Error getting total product count");
                return 0;
            }
        }

        /// <summary>
        /// Düşük stok seviyesindeki ürün sayısını döndürür
        /// </summary>
        public async Task<int> GetLowStockCountAsync()
        {
            try
            {
                return await _context.Products
                    .Where(p => p.Stock <= p.MinimumStock && p.Stock > 0)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Error getting low stock count");
                return 0;
            }
        }

        /// <summary>
        /// Kritik stok seviyesindeki ürün sayısını döndürür
        /// </summary>
        public async Task<int> GetCriticalStockCountAsync()
        {
            try
            {
                return await _context.Products
                    .Where(p => p.Stock <= (p.MinimumStock / 2) && p.Stock > 0)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Error getting critical stock count");
                return 0;
            }
        }

        /// <summary>
        /// Stokta bulunmayan ürün sayısını döndürür
        /// </summary>
        public async Task<int> GetOutOfStockCountAsync()
        {
            try
            {
                return await _context.Products
                    .Where(p => p.Stock <= 0)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Error getting out of stock count");
                return 0;
            }
        }

        /// <summary>
        /// Ürünün güncel stok miktarını döndürür
        /// </summary>
        public async Task<int> GetProductStockAsync(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                return product?.Stock ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Error getting product stock for ID {ProductId}", productId);
                return 0;
            }
        }

        /// <summary>
        /// Ürünün stok durumunu günceller
        /// </summary>
        public async Task<bool> UpdateProductStockAsync(int productId, int quantity, string reason = "Manuel güncelleme")
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    _logger.LogWarning("ALPHA TEAM: Product not found for stock update: {ProductId}", productId);
                    return false;
                }

                var oldStock = product.Stock;
                product.Stock = quantity;
                product.UpdatedAt = DateTime.UtcNow;

                // Stok hareketi kaydet
                await RecordStockMovementAsync(productId, quantity - oldStock,
                    StockMovementType.Adjustment, reason);

                await _context.SaveChangesAsync();

                _logger.LogInformation("ALPHA TEAM: Stock updated for product {ProductId}: {OldStock} -> {NewStock}",
                    productId, oldStock, quantity);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Error updating product stock for ID {ProductId}", productId);
                return false;
            }
        }

        /// <summary>
        /// Stok seviyesi ayarlarını kontrol eder
        /// </summary>
        public async Task<StockStatus> CheckStockLevelAsync(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return StockStatus.OutOfStock;

                if (product.Stock <= 0)
                    return StockStatus.OutOfStock;

                if (product.Stock <= (product.MinimumStock / 2))
                    return StockStatus.Critical;

                if (product.Stock <= product.MinimumStock)
                    return StockStatus.Low;

                return StockStatus.Normal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Error checking stock level for product {ProductId}", productId);
                return StockStatus.OutOfStock;
            }
        }

        /// <summary>
        /// Düşük stoklu ürünlerin listesini döndürür
        /// </summary>
        public async Task<List<Product>> GetLowStockProductsAsync(int threshold = 10)
        {
            try
            {
                return await _context.Products
                    .Where(p => p.Stock <= Math.Max(threshold, p.MinimumStock))
                    .OrderBy(p => p.Stock)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Error getting low stock products");
                return new List<Product>();
            }
        }

        /// <summary>
        /// Stok hareketlerini kaydeder
        /// </summary>
        public async Task<bool> RecordStockMovementAsync(int productId, int changeAmount,
            StockMovementType movementType, string reason)
        {
            try
            {
                var movement = new StockMovement
                {
                    ProductId = productId,
                    ChangeAmount = changeAmount,
                    MovementType = movementType.ToString(),
                    Reason = reason,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System" // TODO: Get from current user context
                };

                _context.StockMovements.Add(movement);
                await _context.SaveChangesAsync();

                _logger.LogInformation("ALPHA TEAM: Stock movement recorded - Product: {ProductId}, Change: {Change}, Type: {Type}",
                    productId, changeAmount, movementType);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Error recording stock movement for product {ProductId}", productId);
                return false;
            }
        }

        /// <summary>
        /// Son stok hareketlerini getirir
        /// </summary>
        public async Task<List<StockMovement>> GetRecentStockMovementsAsync(int limit = 50)
        {
            try
            {
                return await _context.StockMovements
                    .Include(sm => sm.Product)
                    .OrderByDescending(sm => sm.CreatedAt)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Error getting recent stock movements");
                return new List<StockMovement>();
            }
        }
    }
}
