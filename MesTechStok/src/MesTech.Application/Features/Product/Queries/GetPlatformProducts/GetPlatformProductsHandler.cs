using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Product.Queries.GetPlatformProducts;

/// <summary>
/// Pulls products from a specific marketplace adapter via IIntegratorAdapter.PullProductsAsync.
/// Returns paged ProductDto list. G404 wire-up handler.
/// </summary>
public sealed class GetPlatformProductsHandler : IRequestHandler<GetPlatformProductsQuery, PagedResult<ProductDto>>
{
    private readonly IAdapterFactory _adapterFactory;
    private readonly ILogger<GetPlatformProductsHandler> _logger;

    public GetPlatformProductsHandler(IAdapterFactory adapterFactory, ILogger<GetPlatformProductsHandler> logger)
    {
        _adapterFactory = adapterFactory;
        _logger = logger;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetPlatformProductsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var adapter = _adapterFactory.Resolve(request.PlatformCode);
        if (adapter is null)
        {
            _logger.LogWarning("No adapter found for platform {Platform}", request.PlatformCode);
            return PagedResult<ProductDto>.Create([], 0, request.Page, request.PageSize);
        }

        _logger.LogInformation("Pulling products from {Platform} (page {Page})", request.PlatformCode, request.Page);

        var products = await adapter.PullProductsAsync(cancellationToken).ConfigureAwait(false);

        var items = products
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
                CreatedDate = p.CreatedAt,
                ModifiedDate = p.UpdatedAt,
                ProfitMargin = p.ProfitMargin,
                TotalValue = p.TotalValue,
                StockStatus = p.IsOutOfStock() ? "OutOfStock" : p.IsCriticalStock ? "Critical" : p.IsLowStock() ? "Low" : "Normal",
                NeedsReorder = p.NeedsReorder()
            })
            .ToList();

        return PagedResult<ProductDto>.Create(items, products.Count, request.Page, request.PageSize);
    }
}
