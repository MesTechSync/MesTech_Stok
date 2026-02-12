using System.Collections.Generic;
using System.Threading.Tasks;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Services
{
    public interface IProductDataService
    {
        Task<PagedResult<ProductItem>> GetProductsPagedAsync(int page, int pageSize, string? searchTerm, string? categoryFilter, ProductSortOrder sortOrder);
        Task<ProductItem?> GetProductByIdAsync(int id);
        Task<ProductItem?> GetProductByBarcodeAsync(string barcode);
        Task<bool> AddProductAsync(ProductItem product);
        Task<bool> UpdateProductAsync(ProductItem product);
        Task<bool> UpdateFinanceAsync(int productId, decimal? purchasePrice = null, decimal? salePrice = null, decimal? discountRate = null);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> UpdateStockAsync(int productId, int newStock);
        Task<ProductStatistics> GetStatisticsAsync();
        Task<List<string>> GetCategoriesAsync();
        Task<List<string>> SearchCategoriesAsync(string term, int take = 100);
    }
}


