using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetInventoryPaged;

public class GetInventoryPagedHandler : IRequestHandler<GetInventoryPagedQuery, GetInventoryPagedResult>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public GetInventoryPagedHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    public async Task<GetInventoryPagedResult> Handle(GetInventoryPagedQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Fetch products
        var products = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? await _productRepository.GetAllAsync().ConfigureAwait(false)
            : await _productRepository.SearchAsync(request.SearchTerm).ConfigureAwait(false);

        // 2. Build category lookup for name resolution
        var categories = await _categoryRepository.GetAllAsync().ConfigureAwait(false);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name);

        // 3. Apply status filter
        var filtered = ApplyStatusFilter(products, request.StatusFilter);

        // 4. Map to DTOs (manual mapping — no Mapster)
        var dtos = filtered.Select(p => MapToDto(p, categoryLookup)).ToList();

        // 5. Apply sort order
        var sorted = ApplySortOrder(dtos, request.SortOrder);

        // 6. Page the results
        var totalItems = sorted.Count;
        var totalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize);
        var paged = sorted
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new GetInventoryPagedResult
        {
            Items = paged.AsReadOnly(),
            TotalItems = totalItems,
            CurrentPage = request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages
        };
    }

    private static IEnumerable<Product> ApplyStatusFilter(IReadOnlyList<Product> products, StockStatusFilter filter)
    {
        return filter switch
        {
            StockStatusFilter.Normal => products.Where(p => p.Stock > p.MinimumStock),
            StockStatusFilter.Low => products.Where(p => p.Stock <= p.MinimumStock && p.Stock > 5),
            StockStatusFilter.Critical => products.Where(p => p.Stock <= 5 && p.Stock > 0),
            StockStatusFilter.OutOfStock => products.Where(p => p.Stock == 0),
            _ => products
        };
    }

    private static InventoryItemDto MapToDto(Product p, Dictionary<Guid, string> categoryLookup)
    {
        return new InventoryItemDto
        {
            Id = p.Id,
            Barcode = p.Barcode ?? string.Empty,
            ProductName = p.Name,
            Category = categoryLookup.TryGetValue(p.CategoryId, out var categoryName)
                ? categoryName
                : string.Empty,
            Stock = p.Stock,
            MinimumStock = p.MinimumStock,
            Location = p.Location ?? string.Empty,
            Price = p.SalePrice,
            LastMovement = p.UpdatedAt
        };
    }

    private static List<InventoryItemDto> ApplySortOrder(List<InventoryItemDto> items, InventorySortOrder sortOrder)
    {
        return sortOrder switch
        {
            InventorySortOrder.ProductName => items.OrderBy(i => i.ProductName, StringComparer.OrdinalIgnoreCase).ToList(),
            InventorySortOrder.ProductNameDesc => items.OrderByDescending(i => i.ProductName, StringComparer.OrdinalIgnoreCase).ToList(),
            InventorySortOrder.Stock => items.OrderBy(i => i.Stock).ToList(),
            InventorySortOrder.StockDesc => items.OrderByDescending(i => i.Stock).ToList(),
            InventorySortOrder.Category => items.OrderBy(i => i.Category, StringComparer.OrdinalIgnoreCase).ToList(),
            InventorySortOrder.Location => items.OrderBy(i => i.Location, StringComparer.OrdinalIgnoreCase).ToList(),
            InventorySortOrder.LastMovement => items.OrderBy(i => i.LastMovement).ToList(),
            InventorySortOrder.LastMovementDesc => items.OrderByDescending(i => i.LastMovement).ToList(),
            _ => items.OrderBy(i => i.ProductName, StringComparer.OrdinalIgnoreCase).ToList()
        };
    }
}
