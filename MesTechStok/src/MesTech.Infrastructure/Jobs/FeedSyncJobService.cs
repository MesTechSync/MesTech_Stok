using Hangfire;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// IFeedSyncJobService Hangfire implementasyonu.
/// TriggerFeedImportCommandHandler tarafından kullanılır.
/// Sprint-D DEV1 — Dalga 8 Dropshipping aktivasyonu.
/// </summary>
public sealed class FeedSyncJobService : IFeedSyncJobService
{
    /// <inheritdoc />
    public string EnqueueFeedSync(Guid feedId)
    {
        var jobId = BackgroundJob.Enqueue<SupplierFeedSyncJob>(
            job => job.ExecuteAsync(feedId, CancellationToken.None));

        return jobId;
    }
}
