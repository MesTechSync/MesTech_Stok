using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Core.Data;
using MesTechStok.Desktop.Models;
using MesTechStok.Desktop.Utils;

namespace MesTechStok.Desktop.Services
{
    public class SqlBackedProductService : IProductDataService
    {
        private readonly AppDbContext _db;

        public SqlBackedProductService(AppDbContext db)
        {
            _db = db;
        }

        private static string? Clamp(string? s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Length <= max ? s : s.Substring(0, max);
        }

        private static string SerializeWithinLimit(IEnumerable<string> parts, int maxChars)
        {
            var list = new List<string>();
            foreach (var p in parts)
            {
                list.Add(p);
                var json = System.Text.Json.JsonSerializer.Serialize(list);
                if (json.Length > maxChars)
                {
                    list.RemoveAt(list.Count - 1);
                    break;
                }
            }
            return System.Text.Json.JsonSerializer.Serialize(list);
        }

        public async Task<PagedResult<ProductItem>> GetProductsPagedAsync(int page, int pageSize, string? searchTerm, string? categoryFilter, ProductSortOrder sortOrder)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 50;

            // ⚠️ KRITIK: Sadece aktif ürünleri göster (silinen ürünleri gizle)
            IQueryable<MesTechStok.Core.Data.Models.Product> query = _db.Products.AsNoTracking().Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(p => p.Name.Contains(term) || p.Barcode.Contains(term) || p.SKU.Contains(term) || (p.Description != null && p.Description.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != "🗂️ Tüm Kategoriler")
            {
                query = query.Where(p => p.Category != null && p.Category.Name == categoryFilter);
            }

            // Include category only if gerekli alan okunacaksa
            query = query.Include(p => p.Category);

            query = sortOrder switch
            {
                ProductSortOrder.Name => query.OrderBy(p => p.Name),
                ProductSortOrder.NameDesc => query.OrderByDescending(p => p.Name),
                ProductSortOrder.Price => query.OrderBy(p => p.SalePrice),
                ProductSortOrder.PriceDesc => query.OrderByDescending(p => p.SalePrice),
                ProductSortOrder.Stock => query.OrderBy(p => p.Stock),
                ProductSortOrder.StockDesc => query.OrderByDescending(p => p.Stock),
                ProductSortOrder.Category => query.OrderBy(p => p.Category!.Name).ThenBy(p => p.Name),
                ProductSortOrder.CreatedDate => query.OrderBy(p => p.CreatedDate),
                ProductSortOrder.CreatedDateDesc => query.OrderByDescending(p => p.CreatedDate),
                _ => query.OrderBy(p => p.Name)
            };

            var totalItems = await query.CountAsync();
            var resultItems = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    Barcode = p.Barcode,
                    Category = p.Category != null ? p.Category.Name : "",
                    Sku = p.SKU,
                    Price = p.SalePrice,
                    PurchasePrice = p.PurchasePrice,
                    DiscountRate = p.DiscountRate ?? 0m,
                    Stock = p.Stock,
                    MinimumStock = p.MinimumStock,
                    Description = p.Description,
                    Supplier = p.Brand ?? string.Empty,
                    Location = p.Location ?? string.Empty,
                    Origin = p.Origin,
                    Material = p.Material,
                    VolumeText = p.VolumeText,
                    Desi = p.Desi,
                    LeadTimeDays = p.LeadTimeDays,
                    ShipAddress = p.ShipAddress,
                    ReturnAddress = p.ReturnAddress,
                    UsageInstructions = p.UsageInstructions,
                    ImporterInfo = p.ImporterInfo,
                    ManufacturerInfo = p.ManufacturerInfo,
                    Color = p.Color,
                    Sizes = p.Sizes,
                    LengthCm = p.Length,
                    WidthCm = p.Width,
                    HeightCm = p.Height,
                    CreatedDate = p.CreatedDate,
                    LastUpdated = p.ModifiedDate ?? p.CreatedDate,
                    ImageUrl = p.ImageUrl,
                    AdditionalImageUrls = ParseImageUrls(p.ImageUrls)
                })
                .ToListAsync();

            return new PagedResult<ProductItem>
            {
                Items = resultItems,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            };
        }

        public async Task<ProductItem?> GetProductByIdAsync(int id)
        {
            // ⚠️ KRITIK: Sadece aktif ürünleri göster (silinen ürünleri gizle)
            var p = await _db.Products.AsNoTracking().Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
            if (p == null) return null;
            return new ProductItem
            {
                Id = p.Id,
                Name = p.Name,
                Barcode = p.Barcode,
                Category = p.Category?.Name ?? "",
                Sku = p.SKU,
                Price = p.SalePrice,
                PurchasePrice = p.PurchasePrice,
                DiscountRate = p.DiscountRate ?? 0m,
                Stock = p.Stock,
                MinimumStock = p.MinimumStock,
                Description = p.Description,
                Supplier = p.Brand ?? string.Empty,
                Location = p.Location ?? string.Empty,
                Origin = p.Origin,
                Material = p.Material,
                VolumeText = p.VolumeText,
                Desi = p.Desi,
                LeadTimeDays = p.LeadTimeDays,
                ShipAddress = p.ShipAddress,
                ReturnAddress = p.ReturnAddress,
                UsageInstructions = p.UsageInstructions,
                ImporterInfo = p.ImporterInfo,
                ManufacturerInfo = p.ManufacturerInfo,
                Color = p.Color,
                Sizes = p.Sizes,
                LengthCm = p.Length,
                WidthCm = p.Width,
                HeightCm = p.Height,
                CreatedDate = p.CreatedDate,
                LastUpdated = p.ModifiedDate ?? p.CreatedDate,
                ImageUrl = p.ImageUrl,
                AdditionalImageUrls = ParseImageUrls(p.ImageUrls)
            };
        }

        public async Task<ProductItem?> GetProductByBarcodeAsync(string barcode)
        {
            // ⚠️ KRITIK: Sadece aktif ürünleri göster (silinen ürünleri gizle)
            var p = await _db.Products.AsNoTracking().Include(x => x.Category).FirstOrDefaultAsync(x => x.Barcode == barcode && x.IsActive);
            if (p == null) return null;
            return new ProductItem
            {
                Id = p.Id,
                Name = p.Name,
                Barcode = p.Barcode,
                Category = p.Category?.Name ?? "",
                Sku = p.SKU,
                Price = p.SalePrice,
                PurchasePrice = p.PurchasePrice,
                DiscountRate = p.DiscountRate ?? 0m,
                Stock = p.Stock,
                MinimumStock = p.MinimumStock,
                Description = p.Description,
                Supplier = p.Brand ?? string.Empty,
                Location = p.Location ?? string.Empty,
                Origin = p.Origin,
                Material = p.Material,
                VolumeText = p.VolumeText,
                Desi = p.Desi,
                LeadTimeDays = p.LeadTimeDays,
                ShipAddress = p.ShipAddress,
                ReturnAddress = p.ReturnAddress,
                UsageInstructions = p.UsageInstructions,
                ImporterInfo = p.ImporterInfo,
                ManufacturerInfo = p.ManufacturerInfo,
                CreatedDate = p.CreatedDate,
                LastUpdated = p.ModifiedDate ?? p.CreatedDate,
                ImageUrl = p.ImageUrl,
                AdditionalImageUrls = ParseImageUrls(p.ImageUrls)
            };
        }

        public async Task<bool> AddProductAsync(ProductItem product)
        {
            // Esnek: sadece Barkod zorunlu; Ad boşsa barkodu kullan
            if (string.IsNullOrWhiteSpace(product?.Barcode)) return false;
            if (string.IsNullOrWhiteSpace(product.Name)) product.Name = product.Barcode;

            // Aynı barkod zaten varsa: upsert davranışı (güncelle ve dön)
            // ⚠️ KRITIK: Sadece aktif ürünlerde çakışma kontrolü (silinen ürünlerin barkodları yeniden kullanılabilir)
            var existing = await _db.Products.FirstOrDefaultAsync(x => x.Barcode == product.Barcode && x.IsActive);
            if (existing != null)
            {
                product.Id = existing.Id;
                return await UpdateProductAsync(product);
            }

            var entity = new Core.Data.Models.Product
            {
                Name = Clamp(product.Name, 100)!,
                SKU = Clamp(string.IsNullOrWhiteSpace(product.Sku) ? $"AUTO-{Guid.NewGuid():N}".Substring(0, 16) : product.Sku, 50)!,
                Barcode = Clamp(product.Barcode, 50)!,
                Description = Clamp(product.Description ?? string.Empty, 500)!,
                CategoryId = await ResolveCategoryIdAsync(product.Category),
                PurchasePrice = product.PurchasePrice < 0 ? 0 : product.PurchasePrice,
                SalePrice = product.Price < 0 ? 0 : product.Price,
                DiscountRate = product.DiscountRate,
                TaxRate = 0.18m,
                Stock = Math.Max(0, product.Stock),
                MinimumStock = Math.Max(0, product.MinimumStock),
                Location = Clamp(product.Location, 50),
                Brand = string.IsNullOrWhiteSpace(product.Supplier) ? null : Clamp(product.Supplier, 50),
                ImageUrl = Clamp(product.ImageUrl, 255),
                Color = Clamp(product.Color, 50),
                Sizes = Clamp(product.Sizes, 50),
                Length = product.LengthCm,
                Width = product.WidthCm,
                Height = product.HeightCm,
                Origin = Clamp(product.Origin, 50),
                Material = Clamp(product.Material, 50),
                VolumeText = Clamp(product.VolumeText, 50),
                Desi = product.Desi,
                LeadTimeDays = product.LeadTimeDays,
                ShipAddress = Clamp(product.ShipAddress, 255),
                ReturnAddress = Clamp(product.ReturnAddress, 255),
                UsageInstructions = Clamp(product.UsageInstructions, 1000),
                ImporterInfo = Clamp(product.ImporterInfo, 255),
                ManufacturerInfo = Clamp(product.ManufacturerInfo, 255),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            _db.Products.Add(entity);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // SKU benzersiz hatası ise otomatik yeni SKU verip tekrar dene
                if ((ex.InnerException?.Message ?? ex.Message).IndexOf("SKU", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    entity.SKU = Clamp($"AUTO-{Guid.NewGuid():N}".Substring(0, 16), 50)!;
                    await _db.SaveChangesAsync();
                }
                else
                {
                    try
                    {
                        // Kolon eksikliği vb. durumda son bir kez tüm kolonları garanti etmek için context üzerinden çağrı yap
                        await _db.EnsureProductRegulatoryColumnsCreatedAsync();
                        await _db.EnsureProductExtendedColumnsCreatedAsync();
                        await _db.EnsureProductAllColumnsCreatedAsync();
                        await _db.SaveChangesAsync();
                        return true;
                    }
                    catch { return false; }
                }
            }

            try { MesTechStok.Desktop.Views.GlobalLogger.Instance.LogInfo($"[PRODUCT] Ürün eklendi Id={entity.Id}, SKU={entity.SKU}, Barkod={entity.Barcode}", nameof(SqlBackedProductService)); } catch { }

            // Görsel varyantlarını oluştur ve ana görsel yolunu güncelle
            try
            {
                if (!string.IsNullOrWhiteSpace(product.ImageUrl))
                {
                    var storage = new ImageStorageService();
                    var res = await storage.SaveAsync(entity.Id, product.ImageUrl);
                    if (!string.IsNullOrWhiteSpace(res.Full1200))
                    {
                        entity.ImageUrl = Clamp(res.Full1200, 255);
                    }
                    // Ek görselleri JSON olarak kaydet
                    if (!string.IsNullOrWhiteSpace(product.AdditionalImageUrls))
                    {
                        var parts = product.AdditionalImageUrls.Split(new[] { ';', ',', '|', '\\', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        entity.ImageUrls = SerializeWithinLimit(parts, 500);
                    }
                    await _db.SaveChangesAsync();
                }
            }
            catch { }

            return true;
        }

        public async Task<bool> UpdateProductAsync(ProductItem product)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == product.Id);
            if (p == null) return false;

            // Barkod çakışmasında kaydı bloklama: mevcut barkodu koru, diğer alanları güncelle
            var barcodeConflict = !string.IsNullOrWhiteSpace(product.Barcode) && await _db.Products.AnyAsync(x => x.Barcode == product.Barcode && x.Id != product.Id);

            // Ad boşsa barkodu kullan
            if (string.IsNullOrWhiteSpace(product.Name) && !string.IsNullOrWhiteSpace(product.Barcode)) product.Name = product.Barcode;

            p.Name = string.IsNullOrWhiteSpace(product.Name) ? p.Name : Clamp(product.Name, 100)!;
            p.Description = Clamp(product.Description ?? string.Empty, 500)!;
            if (!barcodeConflict && !string.IsNullOrWhiteSpace(product.Barcode)) p.Barcode = product.Barcode;
            p.SKU = string.IsNullOrWhiteSpace(product.Sku) ? p.SKU : Clamp(product.Sku, 50)!;
            p.CategoryId = await ResolveCategoryIdAsync(product.Category);
            var oldPrice = p.SalePrice;
            // Finansal alanlar
            if (product.PurchasePrice > 0) p.PurchasePrice = product.PurchasePrice;
            var oldSale = p.SalePrice;
            p.SalePrice = product.Price;
            p.DiscountRate = product.DiscountRate;
            p.Stock = product.Stock;
            p.MinimumStock = product.MinimumStock;
            p.Location = Clamp(product.Location, 50);
            p.Brand = string.IsNullOrWhiteSpace(product.Supplier) ? null : Clamp(product.Supplier, 50);
            p.ImageUrl = Clamp(product.ImageUrl, 255);
            p.Color = Clamp(product.Color, 50);
            p.Sizes = Clamp(product.Sizes, 50);
            p.Length = product.LengthCm;
            p.Width = product.WidthCm;
            p.Height = product.HeightCm;
            p.Origin = Clamp(product.Origin, 50);
            p.Material = Clamp(product.Material, 50);
            p.VolumeText = Clamp(product.VolumeText, 50);
            p.Desi = product.Desi;
            p.LeadTimeDays = product.LeadTimeDays;
            p.ShipAddress = Clamp(product.ShipAddress, 255);
            p.ReturnAddress = Clamp(product.ReturnAddress, 255);
            p.UsageInstructions = Clamp(product.UsageInstructions, 1000);
            p.ImporterInfo = Clamp(product.ImporterInfo, 255);
            p.ManufacturerInfo = Clamp(product.ManufacturerInfo, 255);
            p.ModifiedDate = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Eşzamanlı güncelleme çakışması: kullanıcıya bilgi ver
                MesTechStok.Desktop.Views.GlobalLogger.Instance.LogWarning("CONCURRENCY_CONFLICT on Product.Update", nameof(SqlBackedProductService));
                return false;
            }
            catch (DbUpdateException ex)
            {
                // SKU benzersiz hatası: eski SKU'yu koru ve tekrar dene
                if ((ex.InnerException?.Message ?? ex.Message).IndexOf("SKU", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // revert SKU
                    var entry = _db.Entry(p);
                    if (entry.Property(nameof(Core.Data.Models.Product.SKU)).IsModified)
                    {
                        entry.Property(nameof(Core.Data.Models.Product.SKU)).CurrentValue = entry.Property(nameof(Core.Data.Models.Product.SKU)).OriginalValue;
                    }
                    await _db.SaveChangesAsync();
                }
                else
                {
                    try
                    {
                        await _db.EnsureProductRegulatoryColumnsCreatedAsync();
                        await _db.EnsureProductExtendedColumnsCreatedAsync();
                        await _db.EnsureProductAllColumnsCreatedAsync();
                        await _db.SaveChangesAsync();
                        return true;
                    }
                    catch { return false; }
                }
            }
            try
            {
                if (oldSale != p.SalePrice)
                {
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("PRODUCT_AUDIT", $"PriceChanged Id={p.Id} Old={oldSale} New={p.SalePrice}", nameof(SqlBackedProductService));
                }
            }
            catch { }
            try { MesTechStok.Desktop.Views.GlobalLogger.Instance.LogInfo($"[PRODUCT] Ürün güncellendi Id={p.Id}, SKU={p.SKU}, Barkod={p.Barcode}", nameof(SqlBackedProductService)); } catch { }

            // Görsel değişmiş olabilir; varyantları güncelle
            try
            {
                if (!string.IsNullOrWhiteSpace(product.ImageUrl))
                {
                    var storage = new ImageStorageService();
                    var res = await storage.SaveAsync(p.Id, product.ImageUrl);
                    if (!string.IsNullOrWhiteSpace(res.Full1200))
                    {
                        p.ImageUrl = Clamp(res.Full1200, 255);
                        await _db.SaveChangesAsync();
                    }
                }
                if (!string.IsNullOrWhiteSpace(product.AdditionalImageUrls))
                {
                    var parts = product.AdditionalImageUrls.Split(new[] { ';', ',', '|', '\\', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    p.ImageUrls = SerializeWithinLimit(parts, 500);
                    await _db.SaveChangesAsync();
                }
            }
            catch { }

            // Entegrasyon: OpenCart stok güncellemesini offline kuyruğa ekle (Out kanal)
            try
            {
                var ocProductId = p.OpenCartProductId;
                if (ocProductId.HasValue)
                {
                    var queue = MesTechStok.Desktop.App.ServiceProvider?.GetService<IOfflineQueueService>();
                    if (queue != null)
                    {
                        var payload = System.Text.Json.JsonSerializer.Serialize(new { ProductId = ocProductId.Value, Quantity = p.Stock });
                        await queue.EnqueueAsync("Stock", "Out", payload, correlationId: $"PROD-{p.Id}");
                        MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("EXPORT", $"Queued stock update for OpenCart ProductId={ocProductId.Value}", "SqlBackedProductService");
                        // Fiyat değişmişse fiyat güncellemesini de kuyruğa ekle
                        if (product.Price != default && Math.Abs(product.Price - oldPrice) > 0.0001m)
                        {
                            var pricePayload = System.Text.Json.JsonSerializer.Serialize(new { ProductId = ocProductId.Value, Price = product.Price });
                            await queue.EnqueueAsync("Product", "Out", pricePayload, correlationId: $"PROD-PRICE-{p.Id}");
                            MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("EXPORT", $"Queued price update for OpenCart ProductId={ocProductId.Value}", "SqlBackedProductService");
                        }
                    }
                }
            }
            catch { }

            return true;
        }

        public async Task<bool> UpdateFinanceAsync(int productId, decimal? purchasePrice = null, decimal? salePrice = null, decimal? discountRate = null)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId);
            if (p == null) return false;
            var oldPurchase = p.PurchasePrice;
            var oldSale2 = p.SalePrice;
            var oldDisc = p.DiscountRate ?? 0;
            if (purchasePrice.HasValue) p.PurchasePrice = Math.Max(0, purchasePrice.Value);
            if (salePrice.HasValue) p.SalePrice = Math.Max(0, salePrice.Value);
            if (discountRate.HasValue) p.DiscountRate = Math.Clamp(discountRate.Value, 0, 100);
            p.ModifiedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            try
            {
                MesTechStok.Desktop.Views.GlobalLogger.Instance.LogInfo($"[PRODUCT] Finans güncellendi Id={p.Id} (Alış={p.PurchasePrice}, Satış={p.SalePrice}, İsk=%{p.DiscountRate})", nameof(SqlBackedProductService));
                if (oldPurchase != p.PurchasePrice)
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("PRODUCT_AUDIT", $"PurchaseChanged Id={p.Id} Old={oldPurchase} New={p.PurchasePrice}", nameof(SqlBackedProductService));
                if (oldSale2 != p.SalePrice)
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("PRODUCT_AUDIT", $"SaleChanged Id={p.Id} Old={oldSale2} New={p.SalePrice}", nameof(SqlBackedProductService));
                if (oldDisc != (p.DiscountRate ?? 0))
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("PRODUCT_AUDIT", $"DiscountChanged Id={p.Id} Old={oldDisc} New={p.DiscountRate}", nameof(SqlBackedProductService));
            }
            catch { }
            return true;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return false;
            p.IsActive = false;
            p.ModifiedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            try { MesTechStok.Desktop.Views.GlobalLogger.Instance.LogInfo($"[PRODUCT] Ürün pasife çekildi Id={p.Id}, SKU={p.SKU}", nameof(SqlBackedProductService)); } catch { }
            return true;
        }

        public async Task<bool> UpdateStockAsync(int productId, int newStock)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId);
            if (p == null) return false;
            var previous = p.Stock;
            p.Stock = Math.Max(0, newStock);
            p.ModifiedDate = DateTime.UtcNow;
            _db.StockMovements.Add(new Core.Data.Models.StockMovement
            {
                ProductId = p.Id,
                Quantity = p.Stock - previous,
                MovementType = (p.Stock - previous) >= 0 ? "IN" : "OUT",
                Date = DateTime.UtcNow,
                Notes = "Manual update",
                PreviousStock = previous,
                NewStock = p.Stock
            });
            await _db.SaveChangesAsync();
            try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("PRODUCT_AUDIT", $"StockChanged Id={p.Id} Old={previous} New={p.Stock}", nameof(SqlBackedProductService)); } catch { }
            try { MesTechStok.Desktop.Views.GlobalLogger.Instance.LogInfo($"[STOCK] Stok güncellendi Id={p.Id}, Önce={previous} Sonra={p.Stock}", nameof(SqlBackedProductService)); } catch { }
            return true;
        }

        public async Task<ProductStatistics> GetStatisticsAsync()
        {
            var totalProducts = await _db.Products.CountAsync(p => p.IsActive);
            var totalValue = await _db.Products.Where(p => p.IsActive).SumAsync(p => p.Stock * p.PurchasePrice);
            var lowStock = await _db.Products.CountAsync(p => p.IsActive && p.Stock <= p.MinimumStock && p.Stock > 0);
            var critical = await _db.Products.CountAsync(p => p.IsActive && p.Stock <= 5 && p.Stock > 0);
            var outOfStock = await _db.Products.CountAsync(p => p.IsActive && p.Stock == 0);
            var avgPrice = await _db.Products.Where(p => p.IsActive).Select(p => p.SalePrice).DefaultIfEmpty(0).AverageAsync();

            return new ProductStatistics
            {
                TotalProducts = totalProducts,
                TotalValue = totalValue,
                LowStockCount = lowStock,
                CriticalStockCount = critical,
                OutOfStockCount = outOfStock,
                AveragePrice = avgPrice
            };
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            return await _db.Categories.AsNoTracking().OrderBy(c => c.Name).Select(c => c.Name).ToListAsync();
        }

        public async Task<List<string>> SearchCategoriesAsync(string term, int take = 100)
        {
            term = (term ?? string.Empty).Trim();
            if (take <= 0) take = 100;
            var query = _db.Categories.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(c => c.Name.StartsWith(term));
            }
            return await query.OrderBy(c => c.Name).Take(take).Select(c => c.Name).ToListAsync();
        }

        private async Task<int> ResolveCategoryIdAsync(string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                var any = await _db.Categories.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync();
                if (any != null) return any.Id;
                // Hiç kategori yoksa otomatik 'Genel' oluştur
                var defaultCat = new Core.Data.Models.Category { Name = "Genel", Code = "GENEL", IsActive = true, CreatedDate = DateTime.UtcNow };
                _db.Categories.Add(defaultCat);
                await _db.SaveChangesAsync();
                return defaultCat.Id;
            }
            var c = await _db.Categories.FirstOrDefaultAsync(x => x.Name == categoryName);
            if (c != null) return c.Id;
            var newCat = new Core.Data.Models.Category { Name = categoryName, Code = $"{categoryName.ToUpperInvariant().Replace(' ', '_')}", IsActive = true, CreatedDate = DateTime.UtcNow };
            _db.Categories.Add(newCat);
            await _db.SaveChangesAsync();
            return newCat.Id;
        }

        private static string ParseImageUrls(string? json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return string.Empty;
                var arr = System.Text.Json.JsonSerializer.Deserialize<string[]>(json);
                if (arr == null || arr.Length == 0) return string.Empty;
                return string.Join(",", arr);
            }
            catch { return string.Empty; }
        }
    }
}


