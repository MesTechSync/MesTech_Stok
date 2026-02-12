using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MesTechStok.Desktop.Data;

namespace MesTechStok.Desktop.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product?> GetProductByBarcodeAsync(string barcode);
        Task<bool> AddProductAsync(Product product);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10);
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
    }
}