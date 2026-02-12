using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Services.Abstract;
using System.Diagnostics;

namespace MesTechStok.Core.Services.Concrete;

/// <summary>
/// Ürün yönetimi servisi implementasyonu
/// Barkodlu stok takip sisteminin temel ürün işlemlerini gerçekleştirir
/// </summary>
public partial class ProductService : IProductService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductService>? _logger;
    private readonly ILoggingService? _loggingService;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public ProductService(AppDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public ProductService(AppDbContext context, ILogger<ProductService> logger, ILoggingService loggingService)
    {
        _context = context;
        _logger = logger;
        _loggingService = loggingService;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.StockMovements)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetProductByBarcodeAsync(string barcode)
    {
        return await _context.Products
            .Include(p => p.StockMovements)
            .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);
    }

    public async Task<Product?> GetProductBySkuAsync(string sku)
    {
        return await _context.Products
            .Include(p => p.StockMovements)
            .FirstOrDefaultAsync(p => p.SKU == sku && p.IsActive);
    }

    public async Task<IEnumerable<Product>> SearchProductsByNameAsync(string searchTerm)
    {
        return await _context.Products
            .Where(p => p.IsActive &&
                       (p.Name.Contains(searchTerm) ||
                        p.Description != null && p.Description.Contains(searchTerm)))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
    {
        return await _context.Products
            .Where(p => p.IsActive && p.Category.Name == category)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive && p.Stock <= p.MinimumStock)
            .OrderBy(p => p.Stock)
            .ToListAsync();
    }

    public async Task<PagedResult<Product>> GetProductsPagedAsync(int page, int pageSize, string? searchTerm = null, string? category = null, string? sortBy = "Name", bool desc = false, bool? inStock = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Barcode.Contains(term) || p.SKU.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim();
            query = query.Where(p => p.Category.Name == cat);
        }

        if (inStock.HasValue)
        {
            query = inStock.Value ? query.Where(p => p.Stock > 0) : query.Where(p => p.Stock == 0);
        }

        // Sıralama
        var ordered = (sortBy?.ToLowerInvariant()) switch
        {
            "saleprice" => (desc ? query.OrderByDescending(p => p.SalePrice) : query.OrderBy(p => p.SalePrice)),
            "stock" => (desc ? query.OrderByDescending(p => p.Stock) : query.OrderBy(p => p.Stock)),
            "createddate" => (desc ? query.OrderByDescending(p => p.CreatedDate) : query.OrderBy(p => p.CreatedDate)),
            "category" => (desc ? query.OrderByDescending(p => p.Category.Name).ThenBy(p => p.Name) : query.OrderBy(p => p.Category.Name).ThenBy(p => p.Name)),
            _ => (desc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name))
        };

        var total = await ordered.CountAsync();
        var items = await ordered
            .Select(p => new Product
            {
                Id = p.Id,
                Name = p.Name,
                Barcode = p.Barcode,
                SKU = p.SKU,
                SalePrice = p.SalePrice,
                PurchasePrice = p.PurchasePrice,
                Stock = p.Stock,
                MinimumStock = p.MinimumStock,
                Description = p.Description,
                Location = p.Location,
                CreatedDate = p.CreatedDate,
                ImageUrl = p.ImageUrl,
                DiscountRate = p.DiscountRate,
                Category = p.Category == null ? null : new Category { Id = p.Category.Id, Name = p.Category.Name },
                Supplier = p.Supplier == null ? null : new Supplier { Id = p.Supplier.Id, Name = p.Supplier.Name }
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger?.LogInformation("Ürün oluşturma işlemi başlatıldı. Barkod: {Barcode}", product.Barcode);
            if (_loggingService != null)
                await _loggingService.LogProductOperationAsync("CREATE_PRODUCT_START", product.Name ?? "Yeni Ürün", product.Barcode);

            // Esnek kayıt politikası: Upsert + zorunlu alan doldurma
            if (string.IsNullOrWhiteSpace(product.Barcode))
            {
                if (_loggingService != null)
                    await _loggingService.LogErrorAsync("Ürün oluşturulamadı: Barkod zorunludur", null, "Product", product);
                throw new InvalidOperationException("Barcode zorunludur.");
            }

            // Varsayılan ad/SKU
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                product.Name = product.Barcode;
                if (_loggingService != null)
                    await _loggingService.LogWarningAsync($"Ürün adı boş, barkod ile dolduruldu: {product.Barcode}", "Product");
            }
            if (string.IsNullOrWhiteSpace(product.SKU))
            {
                product.SKU = $"SKU-{product.Barcode}";
                if (_loggingService != null)
                    await _loggingService.LogWarningAsync($"SKU boş, otomatik oluşturuldu: {product.SKU}", "Product");
            }

            // Kategori zorunluluğunu güvenceye al (Genel kategorisi yoksa oluştur)
            if (product.CategoryId <= 0)
            {
                product.CategoryId = await EnsureGeneralCategoryIdAsync();
                if (_loggingService != null)
                    await _loggingService.LogWarningAsync($"Kategori belirtilmemiş, 'Genel' kategorisi atandı", "Product");
            }

            // Aynı barkod varsa güncelle (upsert)
            var existingByBarcode = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == product.Barcode);
            if (existingByBarcode != null)
            {
                if (_loggingService != null)
                    await _loggingService.LogProductOperationAsync("UPDATE_EXISTING_PRODUCT", existingByBarcode.Name, product.Barcode, new
                    {
                        ExistingProductId = existingByBarcode.Id,
                        UpdatedFields = "Tüm alanlar güncellendi (upsert)"
                    });

                // Mevcut kaydı güncelle
                existingByBarcode.Name = product.Name;
                existingByBarcode.SKU = product.SKU;
                existingByBarcode.Description = product.Description;
                existingByBarcode.PurchasePrice = product.PurchasePrice;
                existingByBarcode.SalePrice = product.SalePrice;
                existingByBarcode.DiscountRate = product.DiscountRate;
                existingByBarcode.Stock = product.Stock;
                existingByBarcode.MinimumStock = product.MinimumStock;
                existingByBarcode.CategoryId = product.CategoryId;
                existingByBarcode.SupplierId = product.SupplierId;
                existingByBarcode.Location = product.Location;
                existingByBarcode.ImageUrl = product.ImageUrl;
                existingByBarcode.ImageUrls = product.ImageUrls;
                existingByBarcode.DocumentUrls = product.DocumentUrls;
                existingByBarcode.Brand = product.Brand;
                existingByBarcode.Model = product.Model;
                existingByBarcode.Color = product.Color;
                existingByBarcode.Size = product.Size;
                existingByBarcode.Sizes = product.Sizes;
                existingByBarcode.Origin = product.Origin;
                existingByBarcode.Material = product.Material;
                existingByBarcode.VolumeText = product.VolumeText;
                existingByBarcode.Desi = product.Desi;
                existingByBarcode.LeadTimeDays = product.LeadTimeDays;
                existingByBarcode.Notes = product.Notes;
                existingByBarcode.Tags = product.Tags;
                existingByBarcode.ModifiedDate = DateTime.UtcNow;
                existingByBarcode.IsActive = true;

                await _context.SaveChangesAsync();

                stopwatch.Stop();
                if (_loggingService != null)
                {
                    await _loggingService.LogPerformanceAsync("CreateProduct_UpsertExisting", stopwatch.Elapsed);
                    await _loggingService.LogProductOperationAsync("CREATE_PRODUCT_SUCCESS_UPSERT", existingByBarcode.Name, product.Barcode, new
                    {
                        ProductId = existingByBarcode.Id,
                        Duration = stopwatch.Elapsed.TotalMilliseconds
                    });
                }

                return existingByBarcode;
            }

            // Yeni kayıt için benzersizlik: SKU çakışması başka ürünle ise düzelt
            if (!await IsSkuUniqueAsync(product.SKU))
            {
                var oldSku = product.SKU;
                product.SKU = $"SKU-{product.Barcode}";
                if (_loggingService != null)
                {
                    await _loggingService.LogWarningAsync($"SKU çakışması düzeltildi: {oldSku} -> {product.SKU}", "Product");
                }
            }

            product.CreatedDate = DateTime.UtcNow;
            product.IsActive = true;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            stopwatch.Stop();
            if (_loggingService != null)
            {
                await _loggingService.LogPerformanceAsync("CreateProduct_NewProduct", stopwatch.Elapsed);
                await _loggingService.LogProductOperationAsync("CREATE_PRODUCT_SUCCESS_NEW", product.Name, product.Barcode, new
                {
                    ProductId = product.Id,
                    SKU = product.SKU,
                    Duration = stopwatch.Elapsed.TotalMilliseconds
                });
            }

            _logger?.LogInformation("Yeni ürün başarıyla oluşturuldu. ID: {ProductId}, Barkod: {Barcode}, Süre: {Duration}ms",
                product.Id, product.Barcode, stopwatch.Elapsed.TotalMilliseconds);

            return product;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            if (_loggingService != null)
            {
                await _loggingService.LogErrorAsync($"Ürün oluşturma hatası: {ex.Message}", ex, "Product", new
                {
                    Barcode = product.Barcode,
                    Name = product.Name,
                    Duration = stopwatch.Elapsed.TotalMilliseconds
                });
            }

            _logger?.LogError(ex, "Ürün oluşturma işlemi başarısız. Barkod: {Barcode}", product.Barcode);
            throw;
        }
    }

    public async Task<Product> UpdateProductAsync(Product product)
    {
        var existingProduct = await GetProductByIdAsync(product.Id);
        if (existingProduct == null)
            throw new InvalidOperationException($"ID '{product.Id}' ile ürün bulunamadı.");

        // Esnek güncelleme: Ad/SKU/Category fallback + barkod başka kayda aitse upsert davranışı
        if (string.IsNullOrWhiteSpace(product.Barcode))
            throw new InvalidOperationException("Barcode zorunludur.");

        if (string.IsNullOrWhiteSpace(product.Name)) product.Name = product.Barcode;
        if (string.IsNullOrWhiteSpace(product.SKU)) product.SKU = $"SKU-{product.Barcode}";
        if (product.CategoryId <= 0) product.CategoryId = await EnsureGeneralCategoryIdAsync();

        var conflict = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == product.Barcode && p.Id != product.Id);
        if (conflict != null)
        {
            // Aynı barkod başka üründeyse, onu güncelle (upsert mantığı)
            conflict.Name = product.Name;
            conflict.SKU = product.SKU;
            conflict.Description = product.Description;
            conflict.PurchasePrice = product.PurchasePrice;
            conflict.SalePrice = product.SalePrice;
            conflict.DiscountRate = product.DiscountRate;
            conflict.Stock = product.Stock;
            conflict.MinimumStock = product.MinimumStock;
            conflict.CategoryId = product.CategoryId;
            conflict.SupplierId = product.SupplierId;
            conflict.Location = product.Location;
            conflict.ImageUrl = product.ImageUrl;
            conflict.ImageUrls = product.ImageUrls;
            conflict.DocumentUrls = product.DocumentUrls;
            conflict.Brand = product.Brand;
            conflict.Model = product.Model;
            conflict.Color = product.Color;
            conflict.Size = product.Size;
            conflict.Sizes = product.Sizes;
            conflict.Origin = product.Origin;
            conflict.Material = product.Material;
            conflict.VolumeText = product.VolumeText;
            conflict.Desi = product.Desi;
            conflict.LeadTimeDays = product.LeadTimeDays;
            conflict.Notes = product.Notes;
            conflict.Tags = product.Tags;
            conflict.ModifiedDate = DateTime.UtcNow;
            conflict.IsActive = true;

            await _context.SaveChangesAsync();
            return conflict;
        }

        product.ModifiedDate = DateTime.UtcNow;
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<bool> DeactivateProductAsync(int id)
    {
        var product = await GetProductByIdAsync(id);
        if (product == null) return false;

        product.IsActive = false;
        product.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateProductAsync(int id)
    {
        var product = await GetProductByIdAsync(id);
        if (product == null) return false;

        product.IsActive = true;
        product.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStockQuantityAsync(int productId, int newQuantity, string? notes = null)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var product = await GetProductByIdAsync(productId);
            if (product == null)
            {
                if (_loggingService != null)
                {
                    await _loggingService.LogWarningAsync($"Stok güncellenemedi: Ürün bulunamadı (ID: {productId})", "Product");
                }
                return false;
            }

            var previousStock = product.Stock;
            var stockChange = newQuantity - previousStock;

            if (_loggingService != null)
            {
                await _loggingService.LogProductOperationAsync("UPDATE_STOCK_START", product.Name, product.Barcode, new
                {
                    ProductId = productId,
                    PreviousStock = previousStock,
                    NewStock = newQuantity,
                    Change = stockChange,
                    Notes = notes
                });
            }

            product.Stock = newQuantity;
            product.ModifiedDate = DateTime.UtcNow;

            // Stok hareket kaydı oluştur
            var stockMovement = new StockMovement
            {
                ProductId = productId,
                MovementType = "ADJUSTMENT",
                Quantity = Math.Abs(stockChange),
                PreviousStock = previousStock,
                NewStock = newQuantity,
                Notes = notes ?? "Stok seviyesi manuel olarak güncellendi",
                Date = DateTime.UtcNow
            };

            _context.StockMovements.Add(stockMovement);
            await _context.SaveChangesAsync();

            stopwatch.Stop();
            if (_loggingService != null)
            {
                await _loggingService.LogPerformanceAsync("UpdateStock", stopwatch.Elapsed);
                await _loggingService.LogProductOperationAsync("UPDATE_STOCK_SUCCESS", product.Name, product.Barcode, new
                {
                    ProductId = productId,
                    PreviousStock = previousStock,
                    NewStock = newQuantity,
                    Change = stockChange,
                    Duration = stopwatch.Elapsed.TotalMilliseconds
                });
            }

            _logger?.LogInformation("Stok güncellendi. Ürün: {ProductName} ({Barcode}), Eski: {OldStock}, Yeni: {NewStock}, Değişim: {Change}",
                product.Name, product.Barcode, previousStock, newQuantity, stockChange);

            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            if (_loggingService != null)
            {
                await _loggingService.LogErrorAsync($"Stok günceleme hatası (ID: {productId}): {ex.Message}", ex, "Product", new
                {
                    ProductId = productId,
                    NewQuantity = newQuantity,
                    Notes = notes,
                    Duration = stopwatch.Elapsed.TotalMilliseconds
                });
            }

            _logger?.LogError(ex, "Stok günceleme başarısız. ProductId: {ProductId}", productId);
            throw;
        }
    }

    public async Task<bool> UpdateProductPriceAsync(int productId, decimal newPrice)
    {
        var product = await GetProductByIdAsync(productId);
        if (product == null) return false;

        product.SalePrice = newPrice;
        product.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeProductId = null)
    {
        var query = _context.Products.Where(p => p.Barcode == barcode);
        if (excludeProductId.HasValue)
            query = query.Where(p => p.Id != excludeProductId.Value);

        return !await query.AnyAsync();
    }

    public async Task<bool> IsSkuUniqueAsync(string sku, int? excludeProductId = null)
    {
        var query = _context.Products.Where(p => p.SKU == sku);
        if (excludeProductId.HasValue)
            query = query.Where(p => p.Id != excludeProductId.Value);

        return !await query.AnyAsync();
    }

    public async Task<IEnumerable<StockMovement>> GetProductStockHistoryAsync(int productId)
    {
        return await _context.StockMovements
            .Where(sm => sm.ProductId == productId)
            .Include(sm => sm.Product)
            .OrderByDescending(sm => sm.Date)
            .ToListAsync();
    }

    public async Task<bool> BulkUpdateProductsAsync(IEnumerable<Product> products)
    {
        try
        {
            foreach (var product in products)
            {
                product.ModifiedDate = DateTime.UtcNow;
            }

            _context.Products.UpdateRange(products);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Toplam aktif ürün sayısını getirir
    /// Dashboard istatistikleri için gerekli
    /// </summary>
    public async Task<int> GetTotalCountAsync()
    {
        try
        {
            return await _context.Products
                .Where(p => p.IsActive)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total product count");
            throw;
        }
    }
}

public partial class ProductService
{
    private async Task<int> EnsureGeneralCategoryIdAsync()
    {
        var cat = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Genel");
        if (cat != null) return cat.Id;
        var newCat = new Category { Name = "Genel", IsActive = true };
        _context.Categories.Add(newCat);
        await _context.SaveChangesAsync();
        return newCat.Id;
    }
}
