using MediatR;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MesTech.Application.Features.Dropshipping.Queries;

public record GetSupplierReliabilityQuery(Guid SupplierFeedId)
    : IRequest<SupplierReliabilityDto?>;

public record SupplierReliabilityDto(
    Guid SupplierFeedId,
    int Score,
    string Color,
    decimal StockAccuracy,
    decimal UpdateFrequency,
    decimal FeedAvailability,
    decimal ProductStability,
    decimal ResponseTime
);

public class GetSupplierReliabilityQueryHandler(
    IFeedReliabilityScoreService scoreService,
    IMemoryCache cache
) : IRequestHandler<GetSupplierReliabilityQuery, SupplierReliabilityDto?>
{
    private const string CacheKeyPrefix = "dropshipping:supplier:score:";

    public async Task<SupplierReliabilityDto?> Handle(
        GetSupplierReliabilityQuery req, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{req.SupplierFeedId}";

        if (cache.TryGetValue(cacheKey, out SupplierReliabilityDto? cached))
            return cached;

        var score = await scoreService.CalculateAsync(req.SupplierFeedId, cancellationToken);
        if (score is null) return null;

        var dto = new SupplierReliabilityDto(
            score.SupplierFeedId,
            score.Score,
            score.Color.ToString(),
            score.StockAccuracy,
            score.UpdateFrequency,
            score.FeedAvailability,
            score.ProductStability,
            score.ResponseTime
        );

        cache.Set(cacheKey, dto, TimeSpan.FromHours(1));
        return dto;
    }
}
