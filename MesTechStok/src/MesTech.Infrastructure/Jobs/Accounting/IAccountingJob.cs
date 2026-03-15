namespace MesTech.Infrastructure.Jobs.Accounting;

/// <summary>
/// Muhasebe modulu Hangfire job'larinin implement ettigi interface.
/// ISyncJob'dan ayri tutulur — muhasebe is mantigi spesifik.
/// </summary>
public interface IAccountingJob
{
    /// <summary>
    /// Job tanimlayicisi (Hangfire recurring job id).
    /// </summary>
    string JobId { get; }

    /// <summary>
    /// Cron ifadesi (orn. "59 23 * * *" = her gun 23:59).
    /// </summary>
    string CronExpression { get; }

    /// <summary>
    /// Job'u calistirir.
    /// </summary>
    Task ExecuteAsync(CancellationToken ct = default);
}
