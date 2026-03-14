using MesTech.Domain.Entities;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Feed import log veri erişim arayüzü.
/// </summary>
public interface IFeedImportLogRepository
{
    Task<(IReadOnlyList<FeedImportLog> Items, int Total)> GetByFeedIdPagedAsync(
        Guid feedId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task AddAsync(FeedImportLog log, CancellationToken ct = default);
    Task UpdateAsync(FeedImportLog log, CancellationToken ct = default);
}
