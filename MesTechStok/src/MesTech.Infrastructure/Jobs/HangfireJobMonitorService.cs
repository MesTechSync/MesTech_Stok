using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job'larin durumunu sorgular.
/// /api/v1/admin/system/jobs endpoint'i bu servisi kullanir.
/// DEV3 TUR9: Katman 5 urun gelistirme — job monitoring dashboard verisi.
/// </summary>
public sealed class HangfireJobMonitorService
{
    private readonly ILogger<HangfireJobMonitorService> _logger;

    public HangfireJobMonitorService(ILogger<HangfireJobMonitorService> logger)
    {
        _logger = logger;
    }

    public HangfireJobDashboard GetDashboard()
    {
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            var recurringJobs = connection.GetRecurringJobs();

            var jobs = recurringJobs.Select(j => new RecurringJobInfo
            {
                JobId = j.Id,
                Cron = j.Cron,
                Queue = j.Queue ?? "default",
                LastExecution = j.LastExecution,
                NextExecution = j.NextExecution,
                LastJobState = j.LastJobState,
                CreatedAt = j.CreatedAt,
                Error = j.Error,
                IsRunning = j.LastJobState == "Processing"
            }).OrderBy(j => j.JobId).ToList();

            return new HangfireJobDashboard
            {
                CheckedAt = DateTime.UtcNow,
                TotalJobs = jobs.Count,
                RunningCount = jobs.Count(j => j.IsRunning),
                FailedCount = jobs.Count(j => j.LastJobState == "Failed"),
                SucceededCount = jobs.Count(j => j.LastJobState == "Succeeded"),
                Jobs = jobs
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hangfire job dashboard query failed — storage may not be initialized");
            return new HangfireJobDashboard
            {
                CheckedAt = DateTime.UtcNow,
                TotalJobs = 0,
                Error = ex.Message
            };
        }
    }
}

public sealed class HangfireJobDashboard
{
    public DateTime CheckedAt { get; init; }
    public int TotalJobs { get; init; }
    public int RunningCount { get; init; }
    public int FailedCount { get; init; }
    public int SucceededCount { get; init; }
    public string? Error { get; init; }
    public List<RecurringJobInfo> Jobs { get; init; } = [];
}

public sealed class RecurringJobInfo
{
    public string JobId { get; init; } = string.Empty;
    public string Cron { get; init; } = string.Empty;
    public string Queue { get; init; } = "default";
    public DateTime? LastExecution { get; init; }
    public DateTime? NextExecution { get; init; }
    public string? LastJobState { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? Error { get; init; }
    public bool IsRunning { get; init; }
}
