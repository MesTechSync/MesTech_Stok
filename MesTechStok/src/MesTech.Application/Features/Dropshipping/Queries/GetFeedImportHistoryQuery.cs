using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Common;

namespace MesTech.Application.Features.Dropshipping.Queries;

public record GetFeedImportHistoryQuery(
    Guid FeedId,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<FeedImportLogDto>>;

public record FeedImportLogDto(
    Guid Id,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalProducts,
    int NewProducts,
    int UpdatedProducts,
    int DeactivatedProducts,
    string Status,
    string? ErrorMessage,
    TimeSpan? Duration
);

public class GetFeedImportHistoryQueryHandler(
    IFeedImportLogRepository logRepo
) : IRequestHandler<GetFeedImportHistoryQuery, PagedResult<FeedImportLogDto>>
{
    public async Task<PagedResult<FeedImportLogDto>> Handle(
        GetFeedImportHistoryQuery req, CancellationToken cancellationToken)
    {
        var (items, total) = await logRepo.GetByFeedIdPagedAsync(
            req.FeedId, req.Page, req.PageSize, cancellationToken);

        var dtos = items.Select(l => new FeedImportLogDto(
            l.Id,
            l.StartedAt,
            l.CompletedAt,
            l.TotalProducts,
            l.NewProducts,
            l.UpdatedProducts,
            l.DeactivatedProducts,
            l.Status.ToString(),
            l.ErrorMessage,
            l.Duration
        )).ToList();

        return PagedResult<FeedImportLogDto>.Create(
            (IReadOnlyList<FeedImportLogDto>)dtos, total, req.Page, req.PageSize);
    }
}
