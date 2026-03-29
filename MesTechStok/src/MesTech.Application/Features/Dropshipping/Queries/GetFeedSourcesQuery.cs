using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Common;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Queries;

public record GetFeedSourcesQuery(
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<FeedSourceDto>>;

public record FeedSourceDto(
    Guid Id,
    string Name,
    string FeedUrl,
    string Format,
    decimal PriceMarkupPercent,
    int SyncIntervalMinutes,
    bool IsActive,
    string LastSyncStatus,
    DateTime? LastSyncAt,
    string? LastSyncError,
    int ProductCount
);

public sealed class GetFeedSourcesQueryHandler(
    ISupplierFeedRepository feedRepo,
    ITenantProvider tenantProvider
) : IRequestHandler<GetFeedSourcesQuery, PagedResult<FeedSourceDto>>
{
    public async Task<PagedResult<FeedSourceDto>> Handle(
        GetFeedSourcesQuery req, CancellationToken cancellationToken)
    {
        var (items, total) = await feedRepo.GetPagedAsync(
            tenantId: tenantProvider.GetCurrentTenantId(),
            isActive: req.IsActive,
            page: req.Page,
            pageSize: req.PageSize,
            ct: cancellationToken);

        var dtos = items.Select(f => new FeedSourceDto(
            f.Id,
            f.Name,
            f.FeedUrl,
            f.Format.ToString(),
            f.PriceMarkupPercent,
            f.SyncIntervalMinutes,
            f.IsActive,
            f.LastSyncStatus.ToString(),
            f.LastSyncAt,
            f.LastSyncError,
            f.LastSyncProductCount
        )).ToList();

        return PagedResult<FeedSourceDto>.Create(
            (IReadOnlyList<FeedSourceDto>)dtos, total, req.Page, req.PageSize);
    }
}
