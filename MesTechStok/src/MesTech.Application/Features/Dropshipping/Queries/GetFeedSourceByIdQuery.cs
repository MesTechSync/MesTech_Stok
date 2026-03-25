using MediatR;
using MesTech.Application.Features.Dropshipping.Queries;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Queries;

public record GetFeedSourceByIdQuery(Guid FeedId) : IRequest<FeedSourceDto?>;

public sealed class GetFeedSourceByIdQueryHandler(
    ISupplierFeedRepository feedRepo
) : IRequestHandler<GetFeedSourceByIdQuery, FeedSourceDto?>
{
    public async Task<FeedSourceDto?> Handle(
        GetFeedSourceByIdQuery req, CancellationToken cancellationToken)
    {
        var feed = await feedRepo.GetByIdAsync(req.FeedId, cancellationToken);
        if (feed is null) return null;

        return new FeedSourceDto(
            feed.Id,
            feed.Name,
            feed.FeedUrl,
            feed.Format.ToString(),
            feed.PriceMarkupPercent,
            feed.SyncIntervalMinutes,
            feed.IsActive,
            feed.LastSyncStatus.ToString(),
            feed.LastSyncAt,
            feed.LastSyncError,
            0
        );
    }
}
