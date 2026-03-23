using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Services
{
    public interface IProductDataService
    {
        Task<PagedResult<ProductItem>> GetProductsPagedAsync(int page, int pageSize, string? searchTerm, string? categoryFilter, ProductSortOrder sortOrder);
        Task<ProductItem?> GetProductByIdAsync(Guid id);
        Task<ProductItem?> GetProductByBarcodeAsync(string barcode);
        Task<bool> AddProductAsync(ProductItem product);
        Task<bool> UpdateProductAsync(ProductItem product);
        Task<bool> UpdateFinanceAsync(Guid productId, decimal? purchasePrice = null, decimal? salePrice = null, decimal? discountRate = null);
        Task<bool> DeleteProductAsync(Guid id);
        Task<bool> UpdateStockAsync(Guid productId, int newStock);
        Task<ProductStatistics> GetStatisticsAsync();
        Task<List<string>> GetCategoriesAsync();
        Task<List<string>> SearchCategoriesAsync(string term, int take = 100);
    }
}


