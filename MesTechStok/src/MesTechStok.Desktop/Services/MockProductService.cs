using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MesTechStok.Desktop.Data;

namespace MesTechStok.Desktop.Services
{
    [Obsolete("DI'da kullanilmiyor — gercek ProductService (Core) aktif. Dalga 2'de kaldirilacak.")]
    public class MockProductService : IProductService
    {
        private readonly List<Product> _products;

        private static readonly Guid _mockCatId1 = Guid.Parse("00000001-0000-0000-0000-000000000001");
        private static readonly Guid _mockCatId2 = Guid.Parse("00000002-0000-0000-0000-000000000002");
        private static readonly Guid _mockCatId3 = Guid.Parse("00000003-0000-0000-0000-000000000003");

        public MockProductService()
        {
            _products = new List<Product>
            {
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Samsung Galaxy S23",
                    SKU = "SAM-GS23-128",
                    Barcode = "1234567890123",
                    Description = "Samsung Galaxy S23 128GB",
                    CategoryId = _mockCatId1,
                    PurchasePrice = 20000m,
                    SalePrice = 25000m,
                    Stock = 45,
                    MinimumStock = 5,
                    Brand = "Samsung",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "iPhone 15 Pro",
                    SKU = "APL-IP15P-256",
                    Barcode = "2345678901234",
                    Description = "Apple iPhone 15 Pro 256GB",
                    CategoryId = _mockCatId1,
                    PurchasePrice = 30000m,
                    SalePrice = 35000m,
                    Stock = 32,
                    MinimumStock = 3,
                    Brand = "Apple",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "MacBook Air M2",
                    SKU = "APL-MBA-M2-512",
                    Barcode = "3456789012345",
                    Description = "Apple MacBook Air M2 512GB",
                    CategoryId = _mockCatId2,
                    PurchasePrice = 25000m,
                    SalePrice = 28000m,
                    Stock = 18,
                    MinimumStock = 2,
                    Brand = "Apple",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Sony WH-1000XM5",
                    SKU = "SON-WH1000XM5",
                    Barcode = "4567890123456",
                    Description = "Sony WH-1000XM5 Kulaklık",
                    CategoryId = _mockCatId3,
                    PurchasePrice = 7000m,
                    SalePrice = 8500m,
                    Stock = 2,
                    MinimumStock = 10,
                    Brand = "Sony",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            };
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            await Task.Delay(100); // Simulate async operation
            return _products.Where(p => p.IsActive).ToList();
        }

        public async Task<Product?> GetProductByIdAsync(Guid id)
        {
            await Task.Delay(50);
            return _products.FirstOrDefault(p => p.Id == id && p.IsActive);
        }

        public async Task<Product?> GetProductByBarcodeAsync(string barcode)
        {
            await Task.Delay(50);
            return _products.FirstOrDefault(p => p.Barcode == barcode && p.IsActive);
        }

        public async Task<bool> AddProductAsync(Product product)
        {
            await Task.Delay(100);

            if (_products.Any(p => p.SKU == product.SKU || p.Barcode == product.Barcode))
                return false;

            product.Id = Guid.NewGuid();
            product.CreatedDate = DateTime.Now;
            _products.Add(product);
            return true;
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            await Task.Delay(100);

            var existingProduct = _products.FirstOrDefault(p => p.Id == product.Id);
            if (existingProduct == null) return false;

            // Check for duplicate SKU/Barcode
            if (_products.Any(p => p.Id != product.Id && (p.SKU == product.SKU || p.Barcode == product.Barcode)))
                return false;

            // Update properties
            existingProduct.Name = product.Name;
            existingProduct.SKU = product.SKU;
            existingProduct.Barcode = product.Barcode;
            existingProduct.Description = product.Description;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.PurchasePrice = product.PurchasePrice;
            existingProduct.SalePrice = product.SalePrice;
            existingProduct.Stock = product.Stock;
            existingProduct.MinimumStock = product.MinimumStock;
            existingProduct.Brand = product.Brand;
            existingProduct.Model = product.Model;
            existingProduct.Color = product.Color;
            existingProduct.Size = product.Size;
            existingProduct.ModifiedDate = DateTime.Now;

            return true;
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            await Task.Delay(50);

            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null) return false;

            product.IsActive = false;
            product.ModifiedDate = DateTime.Now;
            return true;
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId)
        {
            await Task.Delay(50);
            return _products.Where(p => p.CategoryId == categoryId && p.IsActive).ToList();
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10)
        {
            await Task.Delay(50);
            return _products.Where(p => p.Stock <= threshold && p.IsActive).ToList();
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            await Task.Delay(100);

            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllProductsAsync();

            var term = searchTerm.ToLower();
            return _products.Where(p => p.IsActive && (
                p.Name.ToLower().Contains(term) ||
                p.SKU.ToLower().Contains(term) ||
                p.Barcode.Contains(term) ||
                (p.Brand?.ToLower().Contains(term) ?? false) ||
                (p.Description?.ToLower().Contains(term) ?? false)
            )).ToList();
        }
    }
}