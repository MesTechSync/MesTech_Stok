using MesTech.Application.DTOs;
using MesTech.Domain.Common;
using MediatR;

namespace MesTech.Application.Features.Product.Queries.GetProducts;

/// <summary>
/// Paginated product list query — supports search, category filter, stock status filter.
/// Used by ProductListView (Avalonia) and WebApi GET /api/v1/products.
/// </summary>
public record GetProductsQuery(
    Guid TenantId,
    string? SearchTerm = null,
    Guid? CategoryId = null,
    bool? IsActive = null,
    bool? LowStockOnly = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<ProductDto>>;
