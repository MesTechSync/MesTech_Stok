using MesTech.Application.DTOs;
using MesTech.Domain.Common;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Product.Queries.GetProducts;

public sealed class GetProductsHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _productRepo;

    public GetProductsHandler(IProductRepository productRepo) => _productRepo = productRepo;

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var allProducts = !string.IsNullOrWhiteSpace(request.SearchTerm)
            ? await _productRepo.SearchAsync(request.SearchTerm)
            : request.CategoryId.HasValue
                ? await _productRepo.GetByCategoryAsync(request.CategoryId.Value)
                : await _productRepo.GetAllAsync();

        var filtered = allProducts.AsEnumerable();

        if (request.IsActive.HasValue)
            filtered = filtered.Where(p => p.IsActive == request.IsActive.Value);

        if (request.LowStockOnly == true)
            filtered = filtered.Where(p => p.IsLowStock());

        var total = filtered.Count();
        var items = filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                SKU = p.SKU,
                Barcode = p.Barcode,
                PurchasePrice = p.PurchasePrice,
                SalePrice = p.SalePrice,
                ListPrice = p.ListPrice,
                TaxRate = p.TaxRate,
                Stock = p.Stock,
                MinimumStock = p.MinimumStock,
                MaximumStock = p.MaximumStock,
                CategoryId = p.CategoryId,
                SupplierId = p.SupplierId,
                WarehouseId = p.WarehouseId,
                IsActive = p.IsActive,
                ImageUrl = p.ImageUrl,
                Brand = p.Brand,
                Location = p.Location,
                CreatedDate = p.CreatedAt,
                ModifiedDate = p.UpdatedAt,
                ProfitMargin = p.ProfitMargin,
                TotalValue = p.TotalValue,
                StockStatus = p.IsOutOfStock() ? "OutOfStock" : p.IsCriticalStock ? "Critical" : p.IsLowStock() ? "Low" : "Normal",
                NeedsReorder = p.NeedsReorder()
            })
            .ToList();

        return PagedResult<ProductDto>.Create(items, total, request.Page, request.PageSize);
    }
}
