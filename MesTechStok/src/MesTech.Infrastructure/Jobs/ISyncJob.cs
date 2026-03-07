namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Tum recurring sync job'larin implement ettigi interface.
/// </summary>
public interface ISyncJob
{
    string JobId { get; }
    string CronExpression { get; }
    Task ExecuteAsync(CancellationToken ct = default);
}
