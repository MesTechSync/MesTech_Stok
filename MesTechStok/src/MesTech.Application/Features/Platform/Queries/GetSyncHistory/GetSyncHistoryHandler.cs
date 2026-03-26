using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Platform.Queries.GetSyncHistory;

public sealed class GetSyncHistoryHandler : IRequestHandler<GetSyncHistoryQuery, IReadOnlyList<SyncHistoryItemDto>>
{
    private readonly ISyncLogRepository _syncLogRepo;

    public GetSyncHistoryHandler(ISyncLogRepository syncLogRepo) => _syncLogRepo = syncLogRepo;

    public async Task<IReadOnlyList<SyncHistoryItemDto>> Handle(GetSyncHistoryQuery request, CancellationToken ct)
    {
        var logs = await _syncLogRepo.GetRecentAsync(request.TenantId, request.Count, request.PlatformFilter, ct);

        return logs.Select(l => new SyncHistoryItemDto
        {
            Id = l.Id,
            PlatformCode = l.PlatformCode,
            Direction = l.Direction.ToString(),
            EntityType = l.EntityType,
            IsSuccess = l.IsSuccess,
            ErrorMessage = l.ErrorMessage,
            ItemsProcessed = l.ItemsProcessed,
            ItemsFailed = l.ItemsFailed,
            StartedAt = l.StartedAt,
            CompletedAt = l.CompletedAt,
            Duration = l.Duration?.ToString(@"mm\:ss")
        }).ToList().AsReadOnly();
    }
}
