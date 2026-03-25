using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetTopProducts;

/// <summary>
/// En cok satan urunler sorgusu — siparis kalemleri bazinda gelir siralamasi.
/// Cache: 5 dakika.
/// </summary>
public record GetTopProductsQuery(Guid TenantId, int Limit = 10)
    : IRequest<IReadOnlyList<TopProductDto>>, ICacheableQuery
{
    public string CacheKey => $"TopProducts_{TenantId}_{Limit}";
}

/// <summary>
/// En cok satan urun DTO.
/// </summary>
public record TopProductDto
{
    public Guid ProductId { get; init; }
    public string SKU { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int SoldQuantity { get; init; }
    public decimal Revenue { get; init; }
}
