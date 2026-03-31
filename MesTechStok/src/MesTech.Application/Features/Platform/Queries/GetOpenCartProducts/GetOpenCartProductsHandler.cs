using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Platform.Queries.GetOpenCartProducts;

/// <summary>
/// OpenCart platformundan senkronize edilmis urunleri getirir.
/// Store bazinda filtreleme ve sayfalama destekler.
/// </summary>
public sealed class GetOpenCartProductsHandler
    : IRequestHandler<GetOpenCartProductsQuery, GetOpenCartProductsResult>
{
    private readonly IStoreRepository _storeRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<GetOpenCartProductsHandler> _logger;

    public GetOpenCartProductsHandler(
        IStoreRepository storeRepository,
        IProductRepository productRepository,
        ILogger<GetOpenCartProductsHandler> logger)
    {
        _storeRepository = storeRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<GetOpenCartProductsResult> Handle(
        GetOpenCartProductsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var store = await _storeRepository.GetByIdAsync(request.StoreId, cancellationToken).ConfigureAwait(false);
        if (store is null || store.TenantId != request.TenantId || store.PlatformType != PlatformType.OpenCart)
        {
            _logger.LogWarning(
                "OpenCart store bulunamadi veya tenant eslesmedi: StoreId={StoreId}, TenantId={TenantId}",
                request.StoreId, request.TenantId);

            return new GetOpenCartProductsResult
            {
                Products = [],
                TotalCount = 0,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        // Get products mapped to this store via ProductPlatformMapping
        var mappings = store.ProductMappings ?? [];
        var productIds = mappings.Select(m => m.ProductId).Distinct().ToList();

        if (productIds.Count == 0)
        {
            return new GetOpenCartProductsResult
            {
                Products = [],
                TotalCount = 0,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        var allProducts = await _productRepository.GetByIdsAsync(productIds, cancellationToken).ConfigureAwait(false);

        // Apply search filter
        var filtered = allProducts.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            filtered = filtered.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.SKU.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (p.Barcode != null && p.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        var filteredList = filtered.ToList();
        var totalCount = filteredList.Count;

        // Paginate
        var paged = filteredList
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var mappingLookup = mappings.ToDictionary(m => m.ProductId, m => m);

        var dtos = paged.Select(p =>
        {
            var mapping = mappingLookup.GetValueOrDefault(p.Id);
            return new OpenCartProductDto
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU,
                Price = p.SalePrice,
                Quantity = p.Stock,
                Status = p.IsActive ? "Active" : "Inactive",
                LastSyncAt = mapping?.LastSyncDate,
                OpenCartId = mapping?.ExternalProductId
            };
        }).ToList();

        _logger.LogDebug(
            "OpenCart urunleri getirildi: Store={StoreId}, Toplam={Total}, Sayfa={Page}",
            request.StoreId, totalCount, request.Page);

        return new GetOpenCartProductsResult
        {
            Products = dtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
