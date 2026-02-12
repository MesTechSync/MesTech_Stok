using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Desktop.Models;
using MesTechStok.Core.Services.Abstract;

namespace MesTechStok.Desktop.Services
{
    public class EnhancedProductService : IProductDataService
    {
        private readonly List<ProductItem> _allProducts;
        private readonly Random _random = new();
        private readonly ILoggingService? _loggingService;

        public EnhancedProductService()
        {
            _allProducts = GenerateEnhancedDemoData();
            // Dependency Injection'dan logging service'i al (opsiyonel)
            try
            {
                _loggingService = MesTechStok.Desktop.App.ServiceProvider?.GetService<ILoggingService>();
            }
            catch { }
        }

        #region Public Methods

        public async Task<PagedResult<ProductItem>> GetProductsPagedAsync(
            int page = 1,
            int pageSize = 50,
            string? searchTerm = null,
            string? categoryFilter = null,
            ProductSortOrder sortOrder = ProductSortOrder.Name)
        {
            await Task.Delay(50); // Simulate network delay

            var filteredProducts = FilterProducts(searchTerm, categoryFilter);
            var sortedProducts = SortProducts(filteredProducts, sortOrder);

            var totalItems = sortedProducts.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var items = sortedProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<ProductItem>
            {
                Items = items,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<ProductItem?> GetProductByIdAsync(int id)
        {
            await Task.Delay(25);
            return _allProducts.FirstOrDefault(p => p.Id == id);
        }

        public async Task<ProductItem?> GetProductByBarcodeAsync(string barcode)
        {
            await Task.Delay(25);
            return _allProducts.FirstOrDefault(p => p.Barcode == barcode);
        }

        public async Task<bool> AddProductAsync(ProductItem product)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Log başlangıç
                if (_loggingService != null)
                    await _loggingService.LogProductOperationAsync(
                        "CREATE_PRODUCT_START",
                        product.Name ?? "Yeni Ürün",
                        product.Barcode
                    );

                await Task.Delay(100);

                // Validation with detailed logging
                if (string.IsNullOrWhiteSpace(product.Name) || string.IsNullOrWhiteSpace(product.Barcode))
                {
                    if (_loggingService != null)
                        await _loggingService.LogWarningAsync(
                            $"Ürün validasyon hatası - Name veya Barcode boş. Name: '{product.Name}', Barcode: '{product.Barcode}'",
                            "Product"
                        );
                    return false;
                }

                // Check for duplicate barcode with logging
                if (_allProducts.Any(p => p.Barcode == product.Barcode))
                {
                    if (_loggingService != null)
                        await _loggingService.LogWarningAsync(
                            $"Duplicate barcode detected: {product.Barcode}",
                            "Product"
                        );
                    return false;
                }

                // Assign new ID
                product.Id = _allProducts.Max(p => p.Id) + 1;
                product.CreatedDate = DateTime.Now;
                product.LastUpdated = DateTime.Now;

                _allProducts.Add(product);

                stopwatch.Stop();

                // Log başarılı sonuç
                if (_loggingService != null)
                {
                    await _loggingService.LogProductOperationAsync(
                        "CREATE_PRODUCT_SUCCESS",
                        product.Name,
                        product.Barcode,
                        new
                        {
                            ProductId = product.Id,
                            Duration = stopwatch.ElapsedMilliseconds
                        }
                    );

                    await _loggingService.LogPerformanceAsync(
                        "PRODUCT_CREATE",
                        stopwatch.Elapsed,
                        new { ProductId = product.Id, Barcode = product.Barcode }
                    );
                }

                return true;
            }
            catch (Exception ex)
            {
                if (_loggingService != null)
                    await _loggingService.LogErrorAsync(
                        $"Ürün ekleme hatası: {ex.Message}",
                        ex,
                        "Product",
                        product
                    );
                return false;
            }
        }

        public async Task<bool> UpdateProductAsync(ProductItem product)
        {
            await Task.Delay(100);

            var existingProduct = _allProducts.FirstOrDefault(p => p.Id == product.Id);
            if (existingProduct == null) return false;

            // Check for duplicate barcode (excluding current product)
            if (_allProducts.Any(p => p.Barcode == product.Barcode && p.Id != product.Id))
                return false;

            // Update properties
            existingProduct.Name = product.Name;
            existingProduct.Barcode = product.Barcode;
            existingProduct.Category = product.Category;
            existingProduct.Price = product.Price;
            existingProduct.Stock = product.Stock;
            existingProduct.MinimumStock = product.MinimumStock;
            existingProduct.Description = product.Description;
            existingProduct.Supplier = product.Supplier;
            existingProduct.Location = product.Location;
            existingProduct.LastUpdated = DateTime.Now;

            return true;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            await Task.Delay(100);

            var product = _allProducts.FirstOrDefault(p => p.Id == id);
            if (product == null) return false;

            _allProducts.Remove(product);
            return true;
        }

        public async Task<bool> UpdateStockAsync(int productId, int newStock)
        {
            await Task.Delay(50);

            var product = _allProducts.FirstOrDefault(p => p.Id == productId);
            if (product == null) return false;

            product.Stock = newStock;
            product.LastUpdated = DateTime.Now;
            return true;
        }

        public async Task<ProductStatistics> GetStatisticsAsync()
        {
            await Task.Delay(100);

            var totalProducts = _allProducts.Count;
            var totalValue = _allProducts.Sum(p => p.Price * p.Stock);
            var lowStockCount = _allProducts.Count(p => p.Stock <= p.MinimumStock && p.Stock > 0);
            var criticalStockCount = _allProducts.Count(p => p.Stock <= 5);
            var outOfStockCount = _allProducts.Count(p => p.Stock == 0);
            var averagePrice = _allProducts.Any() ? _allProducts.Average(p => p.Price) : 0;

            return new ProductStatistics
            {
                TotalProducts = totalProducts,
                TotalValue = totalValue,
                LowStockCount = lowStockCount,
                CriticalStockCount = criticalStockCount,
                OutOfStockCount = outOfStockCount,
                AveragePrice = averagePrice
            };
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            await Task.Delay(25);
            return _allProducts.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
        }

        public async Task<List<string>> SearchCategoriesAsync(string term, int take = 100)
        {
            await Task.Delay(10);
            term = (term ?? string.Empty).Trim();
            var q = _allProducts.Select(p => p.Category).Distinct();
            if (!string.IsNullOrWhiteSpace(term))
            {
                q = q.Where(c => c.StartsWith(term, StringComparison.OrdinalIgnoreCase));
            }
            return q.OrderBy(c => c).Take(Math.Max(1, take)).ToList();
        }
        public async Task<bool> UpdateFinanceAsync(int productId, decimal? purchasePrice = null, decimal? salePrice = null, decimal? discountRate = null)
        {
            await Task.Delay(25);
            var p = _allProducts.FirstOrDefault(x => x.Id == productId);
            if (p == null) return false;
            if (purchasePrice.HasValue) p.PurchasePrice = Math.Max(0, purchasePrice.Value);
            if (salePrice.HasValue) p.Price = Math.Max(0, salePrice.Value);
            if (discountRate.HasValue) p.DiscountRate = Math.Max(0, Math.Min(100, discountRate.Value));
            p.LastUpdated = DateTime.Now;
            return true;
        }

        #endregion

        #region Private Methods

        private IEnumerable<ProductItem> FilterProducts(string? searchTerm, string? categoryFilter)
        {
            var products = _allProducts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                products = products.Where(p =>
                    p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Barcode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (p.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    p.Supplier.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != "Tüm Kategoriler")
            {
                products = products.Where(p => p.Category.Equals(categoryFilter, StringComparison.OrdinalIgnoreCase));
            }

            return products;
        }

        private IEnumerable<ProductItem> SortProducts(IEnumerable<ProductItem> products, ProductSortOrder sortOrder)
        {
            return sortOrder switch
            {
                ProductSortOrder.Name => products.OrderBy(p => p.Name),
                ProductSortOrder.NameDesc => products.OrderByDescending(p => p.Name),
                ProductSortOrder.Price => products.OrderBy(p => p.Price),
                ProductSortOrder.PriceDesc => products.OrderByDescending(p => p.Price),
                ProductSortOrder.Stock => products.OrderBy(p => p.Stock),
                ProductSortOrder.StockDesc => products.OrderByDescending(p => p.Stock),
                ProductSortOrder.Category => products.OrderBy(p => p.Category).ThenBy(p => p.Name),
                ProductSortOrder.CreatedDate => products.OrderBy(p => p.CreatedDate),
                ProductSortOrder.CreatedDateDesc => products.OrderByDescending(p => p.CreatedDate),
                _ => products.OrderBy(p => p.Name)
            };
        }

        private List<ProductItem> GenerateEnhancedDemoData()
        {
            var products = new List<ProductItem>();
            var categories = new[] { "Elektronik", "İçecek", "Atıştırmalık", "Kozmetik", "Spor", "Gıda", "Oyuncak", "Ev Gereçleri", "Kırtasiye", "Sağlık" };
            var suppliers = new[] { "Tedarikçi A", "Tedarikçi B", "Tedarikçi C", "ABC Ltd.", "XYZ A.Ş.", "Global Tedarik", "Yerel Distribütör" };
            var locations = new[] { "A-01", "A-02", "B-01", "B-02", "C-01", "C-02", "DEPO-1", "DEPO-2", "VİTRİN" };

            var productNames = new Dictionary<string, string[]>
            {
                ["Elektronik"] = new[] { "Samsung Galaxy S24", "iPhone 15 Pro", "MacBook Pro 14\"", "Sony WH-1000XM5", "iPad Air", "Apple Watch Series 9", "Dell XPS 13", "AirPods Pro", "PlayStation 5", "Nintendo Switch" },
                ["İçecek"] = new[] { "Coca Cola 330ml", "Fanta Portakal 330ml", "Sprite 330ml", "Monster Energy 500ml", "Red Bull 250ml", "Çay 100 Adet", "Türk Kahvesi 100gr", "Su 1.5L", "Ayran 200ml", "Meyve Suyu 1L" },
                ["Atıştırmalık"] = new[] { "Doritos Nacho 150g", "Pringles Klasik", "Haribo Jöle", "Ülker Çikolata", "Biskuit 200g", "Fındık 500g", "Çerez Karışımı", "Kuru Üzüm 250g", "Badem 300g", "Fıstık 400g" },
                ["Kozmetik"] = new[] { "Nivea Krem 100ml", "L'Oreal Şampuan", "Johnson's Baby Şampuan", "Garnier Saç Maskesi", "Maybelline Maskara", "Ruj Kırmızı", "Parfüm 50ml", "Nemlendirici", "Güneş Kremi", "Yüz Temizleyici" },
                ["Spor"] = new[] { "Adidas Spor Ayakkabı", "Nike Air Max", "Protein Tozu 1kg", "Yoga Matı", "Dumbbell 5kg", "Spor Çantası", "Koşu Bandı", "Fitness Eldiveni", "Su Matarası", "Spor T-Shirt" },
                ["Gıda"] = new[] { "Nutella 750g", "Zeytinyağı 1L", "Makarna 500g", "Pirinç 1kg", "Un 1kg", "Şeker 1kg", "Tuz 1kg", "Baharat Seti", "Konserve Ton", "Peynir 500g" },
                ["Oyuncak"] = new[] { "Lego City Set", "Hot Wheels Araba Seti", "Barbie Bebek", "Puzzle 1000 Parça", "Teddy Bear", "Oyun Hamuru", "Boyama Kitabı", "Sihirli Küp", "Robot Oyuncak", "Top" },
                ["Ev Gereçleri"] = new[] { "Kahve Makinesi", "Blender", "Tost Makinesi", "Ütü", "Süpürge", "Çaydanlık", "Tencere Seti", "Tabak Seti", "Bardak Seti", "Mutfak Bıçağı" },
                ["Kırtasiye"] = new[] { "Kalem Seti", "Defter A4", "Dosya", "Zımba", "Makas", "Silgi", "Cetvel", "Yapıştırıcı", "Marker Set", "Post-it" },
                ["Sağlık"] = new[] { "Vitamin C", "Omega 3", "Aspirin", "Termometre", "İlk Yardım Çantası", "Kan Basıncı Aleti", "Maske", "Dezenfektan", "Bandaj", "Antiseptik" }
            };

            int idCounter = 1;

            foreach (var category in categories)
            {
                var names = productNames[category];
                foreach (var name in names)
                {
                    var product = new ProductItem
                    {
                        Id = idCounter++,
                        Name = name,
                        Barcode = GenerateBarcode(),
                        Category = category,
                        Price = GenerateRandomPrice(category),
                        Stock = _random.Next(0, 100),
                        MinimumStock = _random.Next(5, 15),
                        Description = $"{name} - Kaliteli ve orijinal ürün",
                        Supplier = suppliers[_random.Next(suppliers.Length)],
                        Location = locations[_random.Next(locations.Length)],
                        CreatedDate = DateTime.Now.AddDays(-_random.Next(1, 365)),
                        LastUpdated = DateTime.Now.AddDays(-_random.Next(0, 30))
                    };
                    products.Add(product);
                }
            }

            // Add extra products for testing pagination
            for (int i = 0; i < 200; i++)
            {
                var category = categories[i % categories.Length];
                var baseNames = productNames[category];
                var product = new ProductItem
                {
                    Id = idCounter++,
                    Name = $"{baseNames[i % baseNames.Length]} - Varyant {i + 1}",
                    Barcode = GenerateBarcode(),
                    Category = category,
                    Price = GenerateRandomPrice(category),
                    Stock = _random.Next(0, 100),
                    MinimumStock = _random.Next(5, 15),
                    Description = $"Test ürünü {i + 1}",
                    Supplier = suppliers[_random.Next(suppliers.Length)],
                    Location = locations[_random.Next(locations.Length)],
                    CreatedDate = DateTime.Now.AddDays(-_random.Next(1, 365)),
                    LastUpdated = DateTime.Now.AddDays(-_random.Next(0, 30))
                };
                products.Add(product);
            }

            return products;
        }

        private string GenerateBarcode()
        {
            return _random.NextInt64(1000000000000, 9999999999999).ToString();
        }

        private decimal GenerateRandomPrice(string category)
        {
            return category switch
            {
                "Elektronik" => (decimal)_random.Next(500, 80000),
                "İçecek" => (decimal)(_random.Next(300, 2000) / 100.0),
                "Atıştırmalık" => (decimal)(_random.Next(500, 3000) / 100.0),
                "Kozmetik" => (decimal)(_random.Next(1000, 10000) / 100.0),
                "Spor" => (decimal)_random.Next(5000, 200000) / 100,
                "Gıda" => (decimal)(_random.Next(1000, 15000) / 100.0),
                "Oyuncak" => (decimal)(_random.Next(2000, 50000) / 100.0),
                "Ev Gereçleri" => (decimal)(_random.Next(5000, 100000) / 100.0),
                "Kırtasiye" => (decimal)(_random.Next(200, 5000) / 100.0),
                "Sağlık" => (decimal)(_random.Next(1000, 25000) / 100.0),
                _ => (decimal)(_random.Next(1000, 10000) / 100.0)
            };
        }

        #endregion
    }

    #region Supporting Classes

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class ProductStatistics
    {
        public int TotalProducts { get; set; }
        public decimal TotalValue { get; set; }
        public int LowStockCount { get; set; }
        public int CriticalStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal AveragePrice { get; set; }
    }

    public enum ProductSortOrder
    {
        Name,
        NameDesc,
        Price,
        PriceDesc,
        Stock,
        StockDesc,
        Category,
        CreatedDate,
        CreatedDateDesc
    }

    #endregion
}