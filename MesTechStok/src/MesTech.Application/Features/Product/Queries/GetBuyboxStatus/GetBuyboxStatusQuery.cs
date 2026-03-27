using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Product.Queries.GetBuyboxStatus;

public record GetBuyboxStatusQuery(Guid TenantId, Guid ProductId, string? PlatformCode = null)
    : IRequest<BuyboxStatusResult>, ICacheableQuery
{
    public string CacheKey => $"Buybox_{TenantId}_{ProductId}_{PlatformCode}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public sealed class BuyboxStatusResult
{
    public Guid ProductId { get; init; }
    public bool IsWinner { get; init; }
    public decimal OurPrice { get; init; }
    public decimal? LowestPrice { get; init; }
    public decimal? BuyboxPrice { get; init; }
    public string? BuyboxSeller { get; init; }
    public int CompetitorCount { get; init; }
    public decimal PriceDifference { get; init; }
    public string Recommendation { get; init; } = string.Empty;
}
